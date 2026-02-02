using Microsoft.Extensions.Logging;

namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Temporarily bypasses tenant isolation. Use with extreme caution.
/// Intended for background jobs and system operations only.
/// All bypass operations are logged for audit purposes.
/// </summary>
public class TenantBypassScope : IDisposable
{
    private readonly TenantContextAccessor _tenantAccessor;
    private readonly ILogger<TenantBypassScope> _logger;
    private readonly Guid? _userId;

    public TenantBypassScope(
        TenantContextAccessor tenantAccessor,
        ILogger<TenantBypassScope> logger)
    {
        _tenantAccessor = tenantAccessor;
        _logger = logger;
        _userId = tenantAccessor.TenantContext.UserId;

        _tenantAccessor.EnableBypass();
        
        // CRITICAL: Log all bypass operations for audit/compliance
        _logger.LogWarning(
            "TENANT BYPASS ENABLED by UserId: {UserId} at {Timestamp}",
            _userId ?? Guid.Empty,
            DateTime.UtcNow);
    }

    public void Dispose()
    {
        _tenantAccessor.DisableBypass();
        
        _logger.LogWarning(
            "TENANT BYPASS DISABLED by UserId: {UserId} at {Timestamp}",
            _userId ?? Guid.Empty,
            DateTime.UtcNow);
    }
}
