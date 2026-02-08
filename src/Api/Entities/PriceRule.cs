using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Defines pricing rules for customers, customer groups, or product groups.
/// Rules are evaluated by priority: CUSTOMER > CUSTOMER_GROUP > PRODUCT_GROUP.
/// </summary>
public class PriceRule : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Scope of the rule: CUSTOMER, CUSTOMER_GROUP, or PRODUCT_GROUP
    /// </summary>
    public string Scope { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of pricing rule: FIXED_PRICE or DISCOUNT_PERCENT
    /// </summary>
    public string RuleType { get; set; } = string.Empty;
    
    /// <summary>
    /// Target entity ID based on scope:
    /// - CUSTOMER: Party.Id
    /// - CUSTOMER_GROUP: Group identifier
    /// - PRODUCT_GROUP: Product group identifier
    /// </summary>
    public Guid TargetId { get; set; }
    
    /// <summary>
    /// Optional variant ID. If null, applies to all variants in scope.
    /// Mutually exclusive with BrandId for simplicity.
    /// </summary>
    public Guid? VariantId { get; set; }
    
    /// <summary>
    /// Optional brand FK filter. If set, applies to all products with this brand.
    /// Mutually exclusive with VariantId for simplicity.
    /// Links to Brand.Id (normalized brand master data).
    /// </summary>
    public Guid? BrandId { get; set; }
    
    /// <summary>
    /// Currency code (e.g., TRY, USD, EUR)
    /// </summary>
    public string Currency { get; set; } = "TRY";
    
    /// <summary>
    /// Rule value:
    /// - For FIXED_PRICE: the fixed price amount
    /// - For DISCOUNT_PERCENT: the discount percentage (e.g., 15.00 for 15%)
    /// </summary>
    public decimal Value { get; set; }
    
    /// <summary>
    /// Priority for rule evaluation. Higher number = higher priority.
    /// Typically: Customer=100, CustomerGroup=50, ProductGroup=10
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Rule validity start date
    /// </summary>
    public DateTime ValidFrom { get; set; }
    
    /// <summary>
    /// Rule validity end date (null = indefinite)
    /// </summary>
    public DateTime? ValidTo { get; set; }
    
    /// <summary>
    /// Rule is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ProductVariant? Variant { get; set; }
    public Brand? BrandNavigation { get; set; }
}
