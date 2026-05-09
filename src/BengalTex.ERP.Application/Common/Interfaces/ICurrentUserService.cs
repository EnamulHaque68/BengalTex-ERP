namespace BengalTex.ERP.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    int? FactoryId { get; }     // Active factory context (multi-factory)
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