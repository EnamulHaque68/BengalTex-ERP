using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

public sealed record UpdateSupplierInvoiceCommand(
    long Id,
    string? SupplierInvoiceNumber,
    decimal VatRate,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<SupplierInvoiceLineInput> Lines
) : IRequest<ApiResponse<SupplierInvoiceDto>>;

public sealed class UpdateSupplierInvoiceCommandValidator : AbstractValidator<UpdateSupplierInvoiceCommand>
{
    public UpdateSupplierInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.SupplierInvoiceNumber).MaximumLength(100);
        RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m)
            .WithMessage("VAT rate must be between 0 (exempt) and 1 (100%).");
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.DueDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A supplier invoice must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.RawMaterialId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("The same raw material appears more than once in the invoice lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class UpdateSupplierInvoiceCommandHandler
    : IRequestHandler<UpdateSupplierInvoiceCommand, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _rawMaterialRepo = rawMaterialRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        UpdateSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);

        if (inv is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier invoice not found.");
        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Draft)
            return ApiResponse<SupplierInvoiceDto>.Fail("Only draft supplier invoices can be edited.");

        var rawMaterialIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existingCount = await _rawMaterialRepo.Query()
            .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
        if (existingCount != rawMaterialIds.Count)
            return ApiResponse<SupplierInvoiceDto>.Fail("One or more raw materials not found.");

        inv.SupplierInvoiceNumber = string.IsNullOrWhiteSpace(cmd.SupplierInvoiceNumber)
            ? null : cmd.SupplierInvoiceNumber.Trim();
        inv.InvoiceDate = cmd.InvoiceDate;
        inv.DueDate = cmd.DueDate;
        inv.Notes = cmd.Notes;
        inv.VatRate = cmd.VatRate;

        inv.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            inv.Lines.Add(new Domain.Entities.SupplierInvoiceLine
            {
                RawMaterialId = line.RawMaterialId,
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

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
