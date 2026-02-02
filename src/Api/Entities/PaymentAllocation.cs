using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Represents allocation of a payment to an invoice
/// Enables partial payment tracking and aging reports based on OpenAmount
/// </summary>
public class PaymentAllocation : TenantEntity
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Party that owns both the invoice and payment
    /// </summary>
    public Guid PartyId { get; set; }
    
    /// <summary>
    /// Invoice being paid
    /// </summary>
    public Guid InvoiceId { get; set; }
    
    /// <summary>
    /// Payment being allocated
    /// </summary>
    public Guid PaymentId { get; set; }
    
    /// <summary>
    /// Currency of the allocation (must match both invoice and payment)
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// Allocated amount (always positive)
    /// Must be <= Payment.UnallocatedAmount and <= Invoice.OpenAmount
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// When the allocation was created
    /// </summary>
    public DateTime AllocatedAt { get; set; }
    
    /// <summary>
    /// Optional note about the allocation
    /// </summary>
    public string? Note { get; set; }
    
    // Navigation properties
    public Party Party { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
