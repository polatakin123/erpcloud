using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Party ledger entry (customer/supplier account movements)
/// Source of truth for party balances
/// </summary>
public class PartyLedgerEntry : TenantEntity
{
    public Guid Id { get; set; }

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
    /// When this entry occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Source type: INVOICE | PAYMENT | INVOICE_CANCEL
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Source document ID (Invoice.Id or Payment.Id)
    /// </summary>
    public Guid SourceId { get; set; }

    /// <summary>
    /// Entry description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Signed amount
    /// Positive: Party owes us (receivable)
    /// Negative: We owe party (payable)
    /// 
    /// Rules:
    /// - Sales Invoice: +GrandTotal
    /// - Purchase Invoice: -GrandTotal
    /// - Payment IN (received): -Amount
    /// - Payment OUT (paid): +Amount
    /// - Invoice Cancel: reverse of original
    /// </summary>
    public decimal AmountSigned { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Open amount (for aging analysis, optional for now)
    /// </summary>
    public decimal? OpenAmountSigned { get; set; }
}
