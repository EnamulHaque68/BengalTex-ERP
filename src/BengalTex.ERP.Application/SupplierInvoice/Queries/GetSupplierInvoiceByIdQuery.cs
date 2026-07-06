using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Queries;

public sealed record GetSupplierInvoiceByIdQuery(long Id) : IRequest<ApiResponse<SupplierInvoiceDto>>;

internal sealed class GetSupplierInvoiceByIdQueryHandler
    : IRequestHandler<GetSupplierInvoiceByIdQuery, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;

    public GetSupplierInvoiceByIdQueryHandler(IRepository<Domain.Entities.SupplierInvoice, long> repo) => _repo = repo;

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        GetSupplierInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .AsNoTracking()
            .Include(s => s.Supplier)
            .Include(s => s.PurchaseOrder)
            .Include(s => s.Currency)
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(s => s.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (inv is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier invoice not found.");

        var lines = inv.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new SupplierInvoiceLineDto(
                l.Id, l.RawMaterialId,
                l.RawMaterial != null ? l.RawMaterial.Code : (l.Account != null ? l.Account.Code : ""),
                l.RawMaterial != null ? l.RawMaterial.Name : (l.Account != null ? l.Account.Name : ""),
                l.RawMaterial != null ? l.RawMaterial.UnitOfMeasure.Code : "",
                l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                l.SortOrder, l.LineNotes,
                l.AccountId,
                l.Account != null ? l.Account.Code : null,
                l.Account != null ? l.Account.Name : null,
                l.AccountId.HasValue))
            .ToList();

        var dto = new SupplierInvoiceDto(
            inv.Id, inv.Code,
            inv.SupplierId, inv.Supplier.Code, inv.Supplier.Name,
            inv.PurchaseOrderId, inv.PurchaseOrder.Code,
            inv.SupplierInvoiceNumber,
            inv.InvoiceDate, inv.DueDate,
            inv.Status.ToString(),
            inv.CurrencyId, inv.Currency.Code, inv.Currency.Symbol, inv.ExchangeRate,
            inv.VatRate, inv.SubtotalAmount, inv.VatAmount,
            inv.TotalAmount, inv.AmountPaid, inv.TotalAmount - inv.AmountPaid,
            inv.TotalAmount * inv.ExchangeRate,
            inv.ApprovedAt, inv.ApprovedBy, inv.Notes,
            lines);

        return ApiResponse<SupplierInvoiceDto>.Ok(dto);
    }
}
