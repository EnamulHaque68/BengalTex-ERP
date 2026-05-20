namespace BengalTex.ERP.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    int? FactoryId { get; }     // Active factory context (multi-factory)
    string? IpAddress { get; }  // Origin of the current request (audit trail)
    string? UserAgent { get; }  // Client user-agent (audit trail)
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
    IReadOnlyList<string> Permissions { get; }
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}