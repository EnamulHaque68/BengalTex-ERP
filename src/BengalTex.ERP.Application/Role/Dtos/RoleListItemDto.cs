namespace BengalTex.ERP.Application.Role.Dtos;

public record RoleListItemDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    int MemberCount,
    int PermissionCount);
