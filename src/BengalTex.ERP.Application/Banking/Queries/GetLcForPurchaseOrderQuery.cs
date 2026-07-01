using BengalTex.ERP.Application.Banking.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Banking.Queries;

/// <summary>
/// The (non-cancelled) letter of credit linked to a purchase order, if any — used by the goods-receipt
/// form to auto-suggest the LC when a PO is selected. Returns null for local / non-LC purchase orders.
/// </summary>
public sealed record GetLcForPurchaseOrderQuery(long PurchaseOrderId)
    : IRequest<ApiResponse<LetterOfCreditListItemDto?>>;

internal sealed class GetLcForPurchaseOrderQueryHandler
    : IRequestHandler<GetLcForPurchaseOrderQuery, ApiResponse<LetterOfCreditListItemDto?>>
{
    private readonly IRepository<LetterOfCredit, long> _repo;

    public GetLcForPurchaseOrderQueryHandler(IRepository<LetterOfCredit, long> repo) => _repo = repo;

    public async Task<ApiResponse<LetterOfCreditListItemDto?>> Handle(
        GetLcForPurchaseOrderQuery request, CancellationToken ct)
    {
        var lc = await _repo.Query().AsNoTracking()
            .Where(l => l.PurchaseOrderId == request.PurchaseOrderId
                        && l.Status != LcStatus.Cancelled)
            .OrderByDescending(l => l.Id)
            .Select(l => new LetterOfCreditListItemDto(
                l.Id, l.Code, l.LcNumber, l.IssuingBank, l.Supplier.Name,
                l.Currency.Code, l.Amount, l.Amount * l.ExchangeRate,
                l.IssueDate, l.ExpiryDate, l.Status.ToString(), l.Type.ToString()))
            .FirstOrDefaultAsync(ct);

        return ApiResponse<LetterOfCreditListItemDto?>.Ok(lc);
    }
}
