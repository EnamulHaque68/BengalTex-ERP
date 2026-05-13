namespace BengalTex.ERP.Application.Permission.Dtos;

/// <summary>
/// Catalog of permissions grouped by their category prefix (e.g., "Customers", "Production").
/// Drives the role-permission picker UI in the admin frontend.
/// </summary>
public record PermissionGroupDto(
    string Category,
    IReadOnlyList<PermissionItemDto> Permissions);

public record PermissionItemDto(
    string Key,           // Full permission constant, e.g., "Customers.View"
    string Action);       // Suffix after category, e.g., "View"
