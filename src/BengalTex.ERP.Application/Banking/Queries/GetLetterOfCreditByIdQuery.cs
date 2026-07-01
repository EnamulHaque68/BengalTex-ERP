using BengalTex.ERP.Application.Banking.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Banking.Queries;

public sealed record GetLetterOfCreditByIdQuery(long Id) : IRequest<ApiResponse<LetterOfCreditDto>>;

internal sealed class GetLetterOfCreditByIdQueryHandler
    : IRequestHandler<GetLetterOfCreditByIdQuery, ApiResponse<LetterOfCreditDto>>
{
    private readonly IRepository<LetterOfCredit, long> _repo;
    private readonly IRepository<GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;

    public GetLetterOfCreditByIdQueryHandler(
        IRepository<LetterOfCredit, long> repo,
        IRepository<GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo)
    {
        _repo = repo;
        _grnRepo = grnRepo;
        _poRepo = poRepo;
    }

    public async Task<ApiResponse<LetterOfCreditDto>> Handle(GetLetterOfCreditByIdQuery request, CancellationToken ct)
    {
        var lc = await _repo.Query().AsNoTracking()
            .Include(l => l.Supplier)
            .Include(l => l.Currency)
            .Include(l => l.PurchaseOrder)
            .FirstOrDefaultAsync(l => l.Id == request.Id, ct);

        if (lc is null) return ApiResponse<LetterOfCreditDto>.Fail("Letter of credit not found.");

        // Goods receipts drawn against this LC (value = Σ received × PO-line price).
        var grnRows = await _grnRepo.Query().AsNoTracking()
            .Where(g => g.LetterOfCreditId == lc.Id)
            .Select(g => new
            {
                g.Id, g.Code, g.Status, g.ReceiveDate,
                Value = g.Lines.Sum(l => (decimal?)(l.ReceivedQuantity * l.PurchaseOrderLine.UnitPrice)) ?? 0m,
                Qty = g.Lines.Sum(l => (decimal?)l.ReceivedQuantity) ?? 0m
            })
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);

        var related = grnRows
            .Select(g => new LcGoodsReceiptRefDto(
                g.Id, g.Code, g.Status.ToString(), g.ReceiveDate, g.Qty, g.Value))
            .ToList();

        // "Received" = POSTED receipts only (actual goods in); drafts are excluded from the summary.
        var posted = grnRows.Where(g => g.Status == GoodsReceiptStatus.Posted).ToList();
        var receivedAmount = posted.Sum(g => g.Value);
        var receivedQty = posted.Sum(g => g.Qty);

        var orderedQty = lc.PurchaseOrderId.HasValue
            ? await _poRepo.Query().AsNoTracking()
                .Where(p => p.Id == lc.PurchaseOrderId.Value)
                .SelectMany(p => p.Lines)
                .SumAsync(l => (decimal?)l.Quantity, ct) ?? 0m
            : 0m;

        var utilization = lc.Amount > 0m ? Math.Round(receivedAmount / lc.Amount * 100m, 2) : 0m;

        var dto = new LetterOfCreditDto(
            lc.Id, lc.Code, lc.LcNumber, lc.IssuingBank,
            lc.SupplierId, lc.Supplier.Name,
            lc.PurchaseOrderId, lc.PurchaseOrder != null ? lc.PurchaseOrder.Code : null,
            lc.CurrencyId, lc.Currency.Code, lc.Currency.Symbol, lc.ExchangeRate,
            lc.Amount, lc.Amount * lc.ExchangeRate,
            lc.IssueDate, lc.ExpiryDate, lc.TenorDays,
            lc.Status.ToString(), lc.Type.ToString(), lc.MasterLcReference, lc.MasterLcBuyer,
            lc.ShipmentDate, lc.SettlementDate, lc.Notes,
            receivedAmount, lc.Amount - receivedAmount, receivedQty, orderedQty, utilization, related);

        return ApiResponse<LetterOfCreditDto>.Ok(dto);
    }
}
