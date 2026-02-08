using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class Product : TenantEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// DEPRECATED: Old text-based brand field. Use BrandId FK instead.
    /// Kept temporarily for migration compatibility. Will be removed after data migration.
    /// </summary>
    [Obsolete("Use BrandId navigation property instead")]
    public string? Brand { get; set; }
    
    /// <summary>
    /// Foreign key to Brand entity (normalized brand master data).
    /// </summary>
    public Guid? BrandId { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation
    public Brand? BrandNavigation { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
