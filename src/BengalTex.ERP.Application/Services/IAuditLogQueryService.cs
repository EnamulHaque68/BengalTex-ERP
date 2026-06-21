using BengalTex.ERP.Application.AuditLog.Dtos;
using BengalTex.ERP.Application.Common.Models;

namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Read-only access to the granular change log (<c>AuditLogEntry</c>). That entity lives
/// in the Infrastructure layer (cross-cutting persistence concern), so the Application
/// layer reaches it through this inverted interface — same pattern as
/// <see cref="IStockService"/> / <see cref="INumberingService"/>.
/// </summary>
public interface IAuditLogQueryService
{
    Task<PagedResult<AuditLogEntryDto>> QueryAsync(
        PagedQueryParameters parameters,
        string? entityType = null,
        string? action = null,
        string? userName = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? entityKey = null,
        CancellationToken ct = default);

    /// <summary>Distinct entity types present in the log — for the filter dropdown.</summary>
    Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default);
}
