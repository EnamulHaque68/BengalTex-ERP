using Microsoft.AspNetCore.Authorization;

namespace BengalTex.ERP.Api.Authorization;

/// <summary>
/// Decorate controller actions with [HasPermission(Permissions.Customers.View)] etc.
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(policy: $"Permission:{permission}") { }
}
