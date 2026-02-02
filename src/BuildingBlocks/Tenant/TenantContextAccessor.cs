namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Scoped accessor for tenant context throughout the request pipeline.
/// </summary>
public class TenantContextAccessor
{
    private readonly TenantContext _tenantContext = new();

    public ITenantContext TenantContext => _tenantContext;

    public void SetTenantId(Guid tenantId)
    {
        _tenantContext.TenantId = tenantId;
    }

    public void SetUserId(Guid userId)
    {
        _tenantContext.UserId = userId;
    }

    public void EnableBypass()
    {
        _tenantContext.IsBypassEnabled = true;
    }

    public void DisableBypass()
    {
        _tenantContext.IsBypassEnabled = false;
    }
}
