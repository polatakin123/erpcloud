using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpCloud.BuildingBlocks.Auditing;

/// <summary>
/// Default audit log writer implementation
/// </summary>
public class AuditLogWriter : IAuditLogWriter
{
    private readonly DbContext _dbContext;
    private readonly ILogger<AuditLogWriter> _logger;

    public AuditLogWriter(DbContext dbContext, ILogger<AuditLogWriter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task WriteAsync(IEnumerable<AuditLog> logs, CancellationToken cancellationToken = default)
    {
        var auditLogs = logs.ToList();
        if (!auditLogs.Any())
            return;

        try
        {
            // Bulk insert audit logs
            await _dbContext.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Successfully wrote {Count} audit logs", auditLogs.Count);
        }
        catch (Exception ex)
        {
            // Swallow audit write errors - don't fail the main transaction
            _logger.LogError(ex, "Failed to write {Count} audit logs. Audit data may be lost.", auditLogs.Count);
        }
    }
}
