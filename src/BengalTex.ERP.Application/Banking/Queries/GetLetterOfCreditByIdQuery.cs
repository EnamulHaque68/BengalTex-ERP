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

    public GetLetterOfCreditByIdQueryHandler(IRepository<LetterOfCredit, long> repo) => _repo = repo;

    public async Task<ApiResponse<LetterOfCreditDto>> Handle(GetLetterOfCreditByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .AsNoTracking()
            .Where(l => l.Id == request.Id)
            .Select(l => new LetterOfCreditDto(
                l.Id, l.Code, l.LcNumber, l.IssuingBank,
                l.SupplierId, l.Supplier.Name,
                l.PurchaseOrderId, l.PurchaseOrder != null ? l.PurchaseOrder.Code : null,
                l.CurrencyId, l.Currency.Code, l.Currency.Symbol, l.ExchangeRate,
                l.Amount, l.Amount * l.ExchangeRate,
                l.IssueDate, l.ExpiryDate, l.TenorDays,
                l.Status.ToString(), l.ShipmentDate, l.SettlementDate, l.Notes))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? ApiResponse<LetterOfCreditDto>.Fail("Letter of credit not found.")
            : ApiResponse<LetterOfCreditDto>.Ok(dto);
    }
}
