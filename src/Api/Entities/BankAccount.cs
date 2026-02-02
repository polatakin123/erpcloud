using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Bank account entity for banking operations.
/// Each tenant can have multiple bank accounts with one default.
/// </summary>
public class BankAccount : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique code within tenant (e.g., "BANK001")
    /// </summary>
    public string Code { get; set; } = null!;
    
    /// <summary>
    /// Account display name
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Bank name (optional)
    /// </summary>
    public string? BankName { get; set; }
    
    /// <summary>
    /// IBAN number (optional, 15-34 chars)
    /// </summary>
    public string? Iban { get; set; }
    
    /// <summary>
    /// Currency code (e.g., "TRY", "USD")
    /// </summary>
    public string Currency { get; set; } = null!;
    
    /// <summary>
    /// Whether this account is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether this is the default bank account for the tenant.
    /// Only one account can be default per tenant (enforced by partial unique constraint).
    /// </summary>
    public bool IsDefault { get; set; }
}
