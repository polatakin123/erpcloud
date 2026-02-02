namespace ErpCloud.BuildingBlocks.Auditing;

/// <summary>
/// Interface for writing audit logs
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// Writes audit logs to storage
    /// </summary>
    Task WriteAsync(IEnumerable<AuditLog> logs, CancellationToken cancellationToken = default);
}
