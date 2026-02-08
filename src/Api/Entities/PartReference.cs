using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Stores alternative reference codes (OEM, aftermarket, supplier codes) for product variants.
/// Used for fast equivalent parts search in spare parts industry.
/// </summary>
public class PartReference : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to ProductVariant
    /// </summary>
    public Guid VariantId { get; set; }
    
    /// <summary>
    /// Reference type: OEM, AFTERMARKET, SUPPLIER, BARCODE
    /// </summary>
    public string RefType { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference code (normalized: uppercase, trimmed)
    /// </summary>
    public string RefCode { get; set; } = string.Empty;
    
    // Navigation
    public ProductVariant? Variant { get; set; }
}
