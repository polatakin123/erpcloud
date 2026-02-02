using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Payment (collection or payment out)
/// </summary>
public class Payment : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Payment number (unique per tenant)
    /// </summary>
    public string PaymentNo { get; set; } = string.Empty;

    /// <summary>
    /// Party (customer/supplier) (FK to Party)
    /// </summary>
    public Guid PartyId { get; set; }
    public Party Party { get; set; } = null!;

    /// <summary>
    /// Branch (FK to Branch)
    /// </summary>
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Payment direction: IN (received) | OUT (paid)
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Payment method: CASH | BANK | CARD | OTHER
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional note
    /// </summary>
    public string? Note { get; set; }
    
    /// <summary>
    /// Source type: CASHBOX or BANK (nullable for backward compatibility)
    /// </summary>
    public string? SourceType { get; set; }
    
    /// <summary>
    /// Source ID (cashbox or bank account) (nullable for backward compatibility)
    /// </summary>
    public Guid? SourceId { get; set; }

    // Payment Allocation Fields (Added for Payment Matching)
    
    /// <summary>
    /// Total amount allocated to invoices
    /// Cache field, recalculated on allocation changes
    /// </summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>
    /// Remaining unallocated amount (Amount - AllocatedAmount)
    /// Cache field, recalculated on allocation changes
    /// </summary>
    public decimal UnallocatedAmount { get; set; }

    /// <summary>
    /// Payment allocations from this payment
    /// </summary>
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
