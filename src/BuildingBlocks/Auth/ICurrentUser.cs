namespace ErpCloud.BuildingBlocks.Auth;

/// <summary>
/// Provides access to current authenticated user information.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Current user's unique identifier (from 'sub' claim).
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Current user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// User's roles from Keycloak (realm_access.roles).
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// User's permissions (custom claim).
    /// </summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// Whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Check if user has a specific role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Check if user has a specific permission.
    /// </summary>
    bool HasPermission(string permission);
}
