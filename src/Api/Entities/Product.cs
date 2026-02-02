using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class Product : TenantEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
