using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Represents a party (customer, supplier, or both) in the system.
/// </summary>
public class Party : TenantEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // CUSTOMER, SUPPLIER, BOTH
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;
}
