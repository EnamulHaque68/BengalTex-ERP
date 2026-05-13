namespace BengalTex.ERP.Application.Role.Dtos;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    int MemberCount,
    IReadOnlyList<string> Permissions);
