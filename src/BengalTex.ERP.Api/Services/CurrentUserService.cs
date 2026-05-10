using System.Security.Claims;
using BengalTex.ERP.Application.Common.Interfaces;

namespace BengalTex.ERP.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("sub")?.Value;

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value
                            ?? User?.FindFirst("unique_name")?.Value;

    public int? FactoryId
    {
        get
        {
            var factoryIdClaim = User?.FindFirst("factoryId")?.Value;
            return int.TryParse(factoryIdClaim, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public bool HasPermission(string permission) =>
        User?.FindAll("permission").Any(c => c.Value == permission) ?? false;

    public IReadOnlyList<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value).ToList() ?? new List<string>();
}