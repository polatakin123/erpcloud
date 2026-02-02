namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Provides access to current tenant and user context.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current tenant identifier. Always set after authentication.
    /// Middleware ensures this is never null at runtime.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Current user identifier. Null if no user context.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Indicates if tenant isolation is bypassed (use with extreme caution).
    /// </summary>
    bool IsBypassEnabled { get; }
}
