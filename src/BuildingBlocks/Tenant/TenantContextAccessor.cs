namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Scoped accessor for tenant context throughout the request pipeline.
/// </summary>
public class TenantContextAccessor
{
    private TenantContext? _tenantContext;

    public ITenantContext TenantContext => _tenantContext ?? throw new InvalidOperationException("TenantContext not initialized");

    internal void SetContext(TenantContext context)
    {
        _tenantContext = context;
    }

    public void SetTenantId(Guid tenantId)
    {
        if (_tenantContext == null)
            throw new InvalidOperationException("TenantContext not initialized");
        
        _tenantContext.TenantId = tenantId;
        Console.WriteLine($"[TenantContextAccessor.SetTenantId] Set to: {tenantId}");
    }

    public void SetUserId(Guid userId)
    {
        if (_tenantContext == null)
            throw new InvalidOperationException("TenantContext not initialized");
            
        _tenantContext.UserId = userId;
    }

    public void EnableBypass()
    {
        if (_tenantContext == null)
            throw new InvalidOperationException("TenantContext not initialized");
            
        _tenantContext.IsBypassEnabled = true;
    }

    public void DisableBypass()
    {
        if (_tenantContext == null)
            throw new InvalidOperationException("TenantContext not initialized");
            
        _tenantContext.IsBypassEnabled = false;
    }
}
