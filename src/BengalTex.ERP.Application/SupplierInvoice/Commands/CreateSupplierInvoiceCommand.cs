using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

/// <summary>One raw-material line submitted with a create/update Supplier Invoice request.</summary>
public sealed record SupplierInvoiceLineInput(
    int RawMaterialId,
    decimal Quantity,
    decimal UnitPrice,
    string? LineNotes);

/// <summary>
/// Records a Draft Supplier Invoice against an existing Purchase Order. The frontend
/// typically pre-populates <see cref="Lines"/> from the PO lines, but the command
/// doesn't enforce that — allows partial invoicing + miscellaneous-charge lines.
/// If <see cref="DueDate"/> is null, defaults to
/// <c>InvoiceDate + Supplier.PaymentTermsDays</c>.
/// </summary>
public sealed record CreateSupplierInvoiceCommand(
    long PurchaseOrderId,
    string? SupplierInvoiceNumber,
    decimal VatRate,                 // 0.0 to 1.0 (e.g. 0.15 for Bangladesh 15%)
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes,
    IReadOnlyList<SupplierInvoiceLineInput> Lines
) : IRequest<ApiResponse<SupplierInvoiceDto>>;

public sealed class CreateSupplierInvoiceCommandValidator : AbstractValidator<CreateSupplierInvoiceCommand>
{
    public CreateSupplierInvoiceCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m)
            .WithMessage("VAT rate must be between 0 (exempt) and 1 (100%).");
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.SupplierInvoiceNumber).MaximumLength(100);
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

internal sealed class CreateSupplierInvoiceCommandHandler
    : IRequestHandler<CreateSupplierInvoiceCommand, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _supplierRepo = supplierRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        CreateSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _poRepo.GetByIdAsync(cmd.PurchaseOrderId, cancellationToken);
        if (po is null) return ApiResponse<SupplierInvoiceDto>.Fail("Purchase order not found.");

        if (po.Status == Domain.Entities.PurchaseOrderStatus.Draft ||
            po.Status == Domain.Entities.PurchaseOrderStatus.Cancelled)
        {
            return ApiResponse<SupplierInvoiceDto>.Fail(
                "Supplier invoice can only be recorded against an Approved (or further) purchase order.");
        }

        var supplier = await _supplierRepo.GetByIdAsync(po.SupplierId, cancellationToken);
        if (supplier is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier not found.");

        var rawMaterialIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existingCount = await _rawMaterialRepo.Query()
            .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
        if (existingCount != rawMaterialIds.Count)
            return ApiResponse<SupplierInvoiceDto>.Fail("One or more raw materials not found.");

        var code = await _numbering.NextAsync("SINV", null, cancellationToken);

        var dueDate = cmd.DueDate ?? cmd.InvoiceDate.AddDays(supplier.PaymentTermsDays);
        var subtotal = cmd.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var vatAmount = Math.Round(subtotal * cmd.VatRate, 4, MidpointRounding.AwayFromZero);
        var total = subtotal + vatAmount;

        var entity = new Domain.Entities.SupplierInvoice
        {
            Code = code,
            SupplierId = po.SupplierId,
            PurchaseOrderId = cmd.PurchaseOrderId,
            SupplierInvoiceNumber = string.IsNullOrWhiteSpace(cmd.SupplierInvoiceNumber)
                ? null : cmd.SupplierInvoiceNumber.Trim(),
            InvoiceDate = cmd.InvoiceDate,
            DueDate = dueDate,
            Status = Domain.Entities.SupplierInvoiceStatus.Draft,
            CurrencyId = po.CurrencyId,          // invoice inherits the PO's currency
            ExchangeRate = po.ExchangeRate,
            VatRate = cmd.VatRate,
            SubtotalAmount = subtotal,
            VatAmount = vatAmount,
            TotalAmount = total,
            AmountPaid = 0m,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.SupplierInvoiceLine
            {
                RawMaterialId = l.RawMaterialId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(entity.Id), cancellationToken);
    }
}
