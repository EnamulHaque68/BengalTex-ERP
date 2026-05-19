using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

public sealed record UpdateCustomerInvoiceCommand(
    long Id,
    decimal VatRate,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<CustomerInvoiceLineInput> Lines
) : IRequest<ApiResponse<CustomerInvoiceDto>>;

public sealed class UpdateCustomerInvoiceCommandValidator : AbstractValidator<UpdateCustomerInvoiceCommand>
{
    public UpdateCustomerInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m)
            .WithMessage("VAT rate must be between 0 (exempt) and 1 (100%).");
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.DueDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A customer invoice must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("The same product appears more than once in the invoice lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class UpdateCustomerInvoiceCommandHandler
    : IRequestHandler<UpdateCustomerInvoiceCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _productRepo = productRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        UpdateCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);

        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Customer invoice not found.");
        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Draft)
            return ApiResponse<CustomerInvoiceDto>.Fail("Only draft customer invoices can be edited.");

        var productIds = cmd.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingCount = await _productRepo.Query()
            .CountAsync(p => productIds.Contains(p.Id), cancellationToken);
        if (existingCount != productIds.Count)
            return ApiResponse<CustomerInvoiceDto>.Fail("One or more products not found.");

        inv.InvoiceDate = cmd.InvoiceDate;
        inv.DueDate = cmd.DueDate;
        inv.Notes = cmd.Notes;
        inv.VatRate = cmd.VatRate;

        // Draft lines carry no history — replace the whole set
        inv.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            inv.Lines.Add(new Domain.Entities.CustomerInvoiceLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        inv.SubtotalAmount = cmd.Lines.Sum(l => l.Quantity * l.UnitPrice);
        inv.VatAmount = Math.Round(inv.SubtotalAmount * inv.VatRate, 4, MidpointRounding.AwayFromZero);
        inv.TotalAmount = inv.SubtotalAmount + inv.VatAmount;

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
