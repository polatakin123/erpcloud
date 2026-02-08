using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Product brand master data (normalized).
/// Examples: Bosch, NGK, Mobil, Castrol, Shell, etc.
/// </summary>
public class Brand : TenantEntity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Normalized brand code (uppercase, unique per tenant).
    /// Examples: "BOSCH", "NGK", "MOBIL"
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the brand.
    /// Examples: "Bosch", "NGK", "Mobil"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional brand logo URL.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Whether this brand is active and can be used.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PriceRule> PriceRules { get; set; } = new List<PriceRule>();
}
