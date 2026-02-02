using Microsoft.AspNetCore.Authorization;

namespace ErpCloud.BuildingBlocks.Auth;

/// <summary>
/// Authorization requirement for permission-based access control
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission, nameof(permission));
        Permission = permission;
    }
}
