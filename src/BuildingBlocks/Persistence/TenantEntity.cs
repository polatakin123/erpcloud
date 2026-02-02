namespace ErpCloud.BuildingBlocks.Persistence;

/// <summary>
/// Base class for all tenant-isolated entities.
/// </summary>
public abstract class TenantEntity
{
    /// <summary>
    /// Tenant identifier for data isolation.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Entity creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created this entity.
    /// </summary>
    public Guid CreatedBy { get; set; }

    // TODO: Add update audit fields when needed:
    // public DateTime? UpdatedAt { get; set; }
    // public Guid? UpdatedBy { get; set; }
    // Update in AppDbContext.SaveChanges when EntityState.Modified
}
