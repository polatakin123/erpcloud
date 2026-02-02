using ErpCloud.BuildingBlocks.Auditing;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ErpCloud.BuildingBlocks.Persistence;

/// <summary>
/// Interceptor to automatically create audit logs on SaveChanges
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuditInterceptor> _logger;

    public AuditInterceptor(ITenantContext tenantContext, ILogger<AuditInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null)
            return;

        try
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId ?? Guid.Empty;

            // Get entries that need auditing (Added/Modified/Deleted)
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .Where(e => e.Entity is TenantEntity) // Only audit tenant entities
                .ToList();

            if (!entries.Any())
                return;

            var auditLogs = AuditHelper.CreateAuditLogs(entries, tenantId, userId);

            if (auditLogs.Any())
            {
                context.Set<AuditLog>().AddRange(auditLogs);
                _logger.LogDebug("Created {Count} audit logs for tenant {TenantId}", auditLogs.Count, tenantId);
            }
        }
        catch (Exception ex)
        {
            // Swallow audit creation errors - don't fail the main transaction
            _logger.LogError(ex, "Failed to create audit logs");
        }
    }
}
