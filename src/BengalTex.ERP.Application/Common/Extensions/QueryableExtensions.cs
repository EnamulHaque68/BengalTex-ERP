using BengalTex.ERP.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQueryParameters parameters,
        CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);
        return PagedResult<T>.Create(items, parameters.Page, parameters.PageSize, totalCount);
    }
}