using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Immutable cash/bank movement ledger entry.
/// Source of truth for all cashbox and bank account movements.
/// Never updated or deleted - only appended.
/// </summary>
public class CashBankLedgerEntry : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// When this movement occurred (timestamptz)
    /// </summary>
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// Type of source: CASHBOX or BANK
    /// </summary>
    public string SourceType { get; set; } = null!;
    
    /// <summary>
    /// ID of the source (cashbox or bank account)
    /// </summary>
    public Guid SourceId { get; set; }
    
    /// <summary>
    /// Reference to payment if this entry was created from a payment
    /// </summary>
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    /// <summary>
    /// Description of the movement
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Signed amount representing the effect on the source's balance.
    /// Positive = increase (e.g., payment IN), Negative = decrease (e.g., payment OUT)
    /// </summary>
    public decimal AmountSigned { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = null!;
}
