namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Default implementation of tenant context.
/// </summary>
public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsBypassEnabled { get; set; }
}
