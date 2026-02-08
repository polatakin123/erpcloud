using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace ErpCloud.BuildingBlocks.Auth;

/// <summary>
/// Implementation of current user context from HTTP claims.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Lazy<List<string>> _roles;
    private readonly Lazy<List<string>> _permissions;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _roles = new Lazy<List<string>>(ParseRoles);
        _permissions = new Lazy<List<string>>(ParsePermissions);
    }

    public string UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst("user_id")?.Value;

            return userIdClaim ?? string.Empty;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Email)?.Value
        ?? _httpContextAccessor.HttpContext?.User
            .FindFirst("email")?.Value;

    public IReadOnlyList<string> Roles => _roles.Value;

    public IReadOnlyList<string> Permissions => _permissions.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    private List<string> ParseRoles()
    {
        var roles = new List<string>();
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null) return roles;

        // Keycloak standard: realm_access.roles
        var realmAccessClaim = user.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccessClaim))
        {
            try
            {
                var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim);
                if (realmAccess.TryGetProperty("roles", out var rolesElement))
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleValue = role.GetString();
                        if (!string.IsNullOrEmpty(roleValue))
                        {
                            roles.Add(roleValue);
                        }
                    }
                }
            }
            catch { /* Invalid JSON */ }
        }

        // Fallback: standard role claims
        var roleClaims = user.FindAll(ClaimTypes.Role)
            .Concat(user.FindAll("role"))
            .Select(c => c.Value)
            .Where(r => !string.IsNullOrEmpty(r));

        roles.AddRange(roleClaims);

        return roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<string> ParsePermissions()
    {
        var permissions = new List<string>();
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null) return permissions;

        // Custom permissions claim (array of strings)
        var permissionsClaim = user.FindFirst("permissions")?.Value;
        if (!string.IsNullOrEmpty(permissionsClaim))
        {
            try
            {
                var perms = JsonSerializer.Deserialize<string[]>(permissionsClaim);
                if (perms != null)
                {
                    permissions.AddRange(perms.Where(p => !string.IsNullOrEmpty(p)));
                }
            }
            catch { /* Invalid JSON */ }
        }

        // Alternative: individual permission claims
        var permClaims = user.FindAll("permission")
            .Select(c => c.Value)
            .Where(p => !string.IsNullOrEmpty(p));

        permissions.AddRange(permClaims);

        // Support "policy" claims as well (for backwards compatibility)
        var policyClaims = user.FindAll("policy")
            .Select(c => c.Value)
            .Where(p => !string.IsNullOrEmpty(p));

        permissions.AddRange(policyClaims);

        return permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
