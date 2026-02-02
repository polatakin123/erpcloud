namespace ErpCloud.BuildingBlocks.Auditing;

/// <summary>
/// Audit log entity for tracking all CRUD operations
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tenant identifier for multi-tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// User who performed the action (Guid.Empty if system action)
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Entity type name (e.g., "Product", "Order")
    /// </summary>
    public required string EntityName { get; set; }
    
    /// <summary>
    /// Entity identifier (Guid as string)
    /// </summary>
    public required string EntityId { get; set; }
    
    /// <summary>
    /// Action performed (Created/Updated/Deleted)
    /// </summary>
    public AuditAction Action { get; set; }
    
    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// JSON diff containing changes:
    /// - Created: { "after": {...} }
    /// - Updated: { "before": {...}, "after": {...} } (only changed fields)
    /// - Deleted: { "before": {...} }
    /// </summary>
    public required string DiffJson { get; set; }
    
    /// <summary>
    /// Optional correlation ID for request tracking
    /// </summary>
    public string? CorrelationId { get; set; }
}
