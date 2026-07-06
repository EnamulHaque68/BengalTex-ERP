using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.LandedCost;

/// <summary>Shared allocation math: spread a voucher's total charges across the GRN lines.</summary>
internal static class LandedCostAllocator
{
    public sealed record LineWeight(int RawMaterialId, string RawMaterialCode, string RawMaterialName,
        decimal ReceivedQuantity, decimal LineValue);

    /// <summary>
    /// Returns (line, allocatedAmount) for every GRN line. Weight = line value (qty × price × rate)
    /// for ByValue, else received quantity. Falls back to equal split when every weight is zero.
    /// </summary>
    public static List<(LineWeight Line, decimal Allocated)> Allocate(
        IReadOnlyList<LineWeight> lines, decimal totalCharges, LandedCostAllocationBasis basis)
    {
        var result = new List<(LineWeight, decimal)>();
        if (lines.Count == 0 || totalCharges <= 0m)
        {
            foreach (var l in lines) result.Add((l, 0m));
            return result;
        }

        decimal Weight(LineWeight l) => basis == LandedCostAllocationBasis.ByQuantity ? l.ReceivedQuantity : l.LineValue;
        var totalWeight = lines.Sum(Weight);

        decimal running = 0m;
        for (var i = 0; i < lines.Count; i++)
        {
            decimal alloc;
            if (i == lines.Count - 1)
                alloc = totalCharges - running;                       // last line absorbs the rounding remainder
            else
            {
                var w = totalWeight > 0m ? Weight(lines[i]) / totalWeight : 1m / lines.Count;
                alloc = Math.Round(totalCharges * w, 2, MidpointRounding.AwayFromZero);
                running += alloc;
            }
            result.Add((lines[i], alloc));
        }
        return result;
    }
}

public sealed record GetLandedCostVouchersQuery(PagedQueryParameters Parameters, string? Status = null)
    : IRequest<ApiResponse<PagedResult<LandedCostVoucherListItemDto>>>;

internal sealed class GetLandedCostVouchersQueryHandler
    : IRequestHandler<GetLandedCostVouchersQuery, ApiResponse<PagedResult<LandedCostVoucherListItemDto>>>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    public GetLandedCostVouchersQueryHandler(IRepository<LandedCostVoucher, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<LandedCostVoucherListItemDto>>> Handle(
        GetLandedCostVouchersQuery req, CancellationToken ct)
    {
        var q = _repo.Query()
            .Include(v => v.GoodsReceiptNote).ThenInclude(g => g.PurchaseOrder).ThenInclude(p => p.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<LandedCostVoucherStatus>(req.Status, true, out var s))
            q = q.Where(v => v.Status == s);
        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(v => v.Code.Contains(search) || v.GoodsReceiptNote.Code.Contains(search));

        q = q.OrderByDescending(v => v.Id);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((req.Parameters.Page - 1) * req.Parameters.PageSize).Take(req.Parameters.PageSize)
            .Select(v => new LandedCostVoucherListItemDto(
                v.Id, v.Code, v.VoucherDate, v.GoodsReceiptNote.Code,
                v.GoodsReceiptNote.PurchaseOrder.Supplier.Name,
                v.AllocationBasis.ToString(), v.Status.ToString(),
                v.Charges.Count, v.Charges.Sum(c => c.Amount),
                v.IsOnCredit, v.SettledAt != null))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<LandedCostVoucherListItemDto>>.Ok(
            PagedResult<LandedCostVoucherListItemDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

public sealed record GetLandedCostVoucherByIdQuery(long Id) : IRequest<ApiResponse<LandedCostVoucherDto>>;

internal sealed class GetLandedCostVoucherByIdQueryHandler
    : IRequestHandler<GetLandedCostVoucherByIdQuery, ApiResponse<LandedCostVoucherDto>>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    public GetLandedCostVoucherByIdQueryHandler(IRepository<LandedCostVoucher, long> repo) => _repo = repo;

    public async Task<ApiResponse<LandedCostVoucherDto>> Handle(GetLandedCostVoucherByIdQuery req, CancellationToken ct)
    {
        var v = await _repo.Query().AsNoTracking()
            .Include(x => x.Charges)
            .Include(x => x.Supplier)
            .Include(x => x.GoodsReceiptNote).ThenInclude(g => g.PurchaseOrder).ThenInclude(p => p.Supplier)
            .Include(x => x.GoodsReceiptNote).ThenInclude(g => g.Lines).ThenInclude(l => l.PurchaseOrderLine).ThenInclude(pl => pl.RawMaterial)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (v is null) return ApiResponse<LandedCostVoucherDto>.Fail("Landed-cost voucher not found.");

        var grn = v.GoodsReceiptNote;
        var rate = grn.PurchaseOrder.ExchangeRate;
        var lineWeights = grn.Lines.OrderBy(l => l.SortOrder).Select(l => new LandedCostAllocator.LineWeight(
            l.PurchaseOrderLine.RawMaterialId, l.PurchaseOrderLine.RawMaterial.Code, l.PurchaseOrderLine.RawMaterial.Name,
            l.ReceivedQuantity, l.ReceivedQuantity * l.PurchaseOrderLine.UnitPrice * rate)).ToList();

        var total = v.Charges.Sum(c => c.Amount);
        var allocation = LandedCostAllocator.Allocate(lineWeights, total, v.AllocationBasis)
            .Select(a => new LandedCostAllocationLineDto(
                a.Line.RawMaterialId, a.Line.RawMaterialCode, a.Line.RawMaterialName,
                a.Line.ReceivedQuantity, a.Line.LineValue, a.Allocated,
                a.Line.ReceivedQuantity > 0m ? Math.Round(a.Allocated / a.Line.ReceivedQuantity, 4, MidpointRounding.AwayFromZero) : 0m))
            .ToList();

        var charges = v.Charges.OrderBy(c => c.SortOrder)
            .Select(c => new LandedCostChargeDto(c.Id, c.ChargeType.ToString(), c.Amount, c.Notes, c.SortOrder))
            .ToList();

        return ApiResponse<LandedCostVoucherDto>.Ok(new LandedCostVoucherDto(
            v.Id, v.Code, v.VoucherDate, grn.Id, grn.Code, grn.PurchaseOrder.Code, grn.PurchaseOrder.Supplier.Name,
            v.AllocationBasis.ToString(), v.PaymentMethod.ToString(), v.Status.ToString(),
            v.PostedAt, v.PostedBy, v.Notes, total, charges, allocation,
            v.IsOnCredit, v.SupplierId, v.Supplier != null ? v.Supplier.Name : null,
            v.SettledDate, v.SettledBy, v.SettlementMethod != null ? v.SettlementMethod.ToString() : null,
            v.SettledAt != null));
    }
}
