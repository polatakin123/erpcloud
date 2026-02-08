using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Stores cost information for product variants to enable profit calculation.
/// </summary>
public class ProductCost : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Product variant reference
    /// </summary>
    public Guid VariantId { get; set; }
    
    /// <summary>
    /// Last recorded purchase cost for this variant
    /// </summary>
    public decimal LastPurchaseCost { get; set; }
    
    /// <summary>
    /// Average cost if using moving average costing method
    /// </summary>
    public decimal? AverageCost { get; set; }
    
    /// <summary>
    /// Minimum allowed sale price (optional floor price)
    /// </summary>
    public decimal? MinSalePrice { get; set; }
    
    /// <summary>
    /// Currency for cost values
    /// </summary>
    public string Currency { get; set; } = "TRY";
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ProductVariant Variant { get; set; } = null!;
}
