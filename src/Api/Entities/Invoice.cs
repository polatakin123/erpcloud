using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Sales or Purchase Invoice
/// </summary>
public class Invoice : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Invoice number (unique per tenant)
    /// </summary>
    public string InvoiceNo { get; set; } = string.Empty;

    /// <summary>
    /// Invoice type: SALES | PURCHASE
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Customer or Supplier (FK to Party)
    /// </summary>
    public Guid PartyId { get; set; }
    public Party Party { get; set; } = null!;

    /// <summary>
    /// Branch (FK to Branch)
    /// </summary>
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    /// <summary>
    /// Invoice issue date
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Payment due date (optional)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Currency code (e.g., TRY, USD, EUR)
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Invoice status: DRAFT | ISSUED | CANCELLED
    /// </summary>
    public string Status { get; set; } = "DRAFT";

    /// <summary>
    /// Source type for invoice origin (e.g., SHIPMENT)
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// Source record ID (e.g., ShipmentId if SourceType=SHIPMENT)
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Subtotal (sum of line totals, excluding VAT)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Total VAT amount
    /// </summary>
    public decimal VatTotal { get; set; }

    /// <summary>
    /// Grand total (Subtotal + VatTotal)
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Optional note
    /// </summary>
    public string? Note { get; set; }

    // Payment Allocation Fields (Added for Payment Matching)
    
    /// <summary>
    /// Total amount paid towards this invoice (sum of allocations)
    /// Cache field, recalculated on allocation changes
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Remaining amount to be paid (GrandTotal - PaidAmount)
    /// Cache field, recalculated on allocation changes
    /// </summary>
    public decimal OpenAmount { get; set; }

    /// <summary>
    /// Payment status: OPEN | PARTIAL | PAID
    /// Calculated based on PaidAmount vs GrandTotal
    /// </summary>
    public string PaymentStatus { get; set; } = "OPEN";

    /// <summary>
    /// Invoice lines
    /// </summary>
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    
    /// <summary>
    /// Payment allocations to this invoice
    /// </summary>
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
