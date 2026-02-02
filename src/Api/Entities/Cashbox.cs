using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Cashbox (kasa) entity for cash management.
/// Each tenant can have multiple cashboxes with one default.
/// </summary>
public class Cashbox : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique code within tenant (e.g., "CASH001")
    /// </summary>
    public string Code { get; set; } = null!;
    
    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Currency code (e.g., "TRY", "USD")
    /// </summary>
    public string Currency { get; set; } = null!;
    
    /// <summary>
    /// Whether this cashbox is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether this is the default cashbox for the tenant.
    /// Only one cashbox can be default per tenant (enforced by partial unique constraint).
    /// </summary>
    public bool IsDefault { get; set; }
}
