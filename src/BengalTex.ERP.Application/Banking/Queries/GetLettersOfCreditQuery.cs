using BengalTex.ERP.Application.Banking.Dtos;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Banking.Queries;

public sealed record GetLettersOfCreditQuery(
    PagedQueryParameters Parameters,
    int? SupplierId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<LetterOfCreditListItemDto>>>;

internal sealed class GetLettersOfCreditQueryHandler
    : IRequestHandler<GetLettersOfCreditQuery, ApiResponse<PagedResult<LetterOfCreditListItemDto>>>
{
    private readonly IRepository<LetterOfCredit, long> _repo;

    public GetLettersOfCreditQueryHandler(IRepository<LetterOfCredit, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<LetterOfCreditListItemDto>>> Handle(
        GetLettersOfCreditQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (request.SupplierId.HasValue)
            query = query.Where(l => l.SupplierId == request.SupplierId.Value);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<LcStatus>(request.Status, out var status))
        {
            query = query.Where(l => l.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l =>
                l.Code.Contains(search) ||
                l.LcNumber.Contains(search) ||
                l.IssuingBank.Contains(search) ||
                l.Supplier.Name.Contains(search));
        }

        query = query.OrderByDescending(l => l.Id);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(l => new LetterOfCreditListItemDto(
                l.Id, l.Code, l.LcNumber, l.IssuingBank, l.Supplier.Name,
                l.Currency.Code, l.Amount, l.Amount * l.ExchangeRate,
                l.IssueDate, l.ExpiryDate, l.Status.ToString(), l.Type.ToString()))
            .ToListAsync(ct);

        var result = PagedResult<LetterOfCreditListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<LetterOfCreditListItemDto>>.Ok(result);
    }
}
