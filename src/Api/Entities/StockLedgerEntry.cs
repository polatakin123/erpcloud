using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Immutable stock movement ledger entry.
/// Source of truth for all stock movements.
/// Never updated or deleted - only appended.
/// </summary>
public class StockLedgerEntry : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// When this movement occurred (timestamptz)
    /// </summary>
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// Which warehouse this movement affects
    /// </summary>
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    /// <summary>
    /// Which product variant (SKU) this movement is for
    /// </summary>
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    
    /// <summary>
    /// Type of movement:
    /// - INBOUND: Stock receipt (+qty affects OnHand)
    /// - OUTBOUND: Stock issue (-qty affects OnHand)
    /// - ADJUSTMENT: Stock count adjustment (+/- affects OnHand)
    /// - RESERVE: Reservation for order (+qty affects Reserved)
    /// - RELEASE: Release reservation (-qty affects Reserved)
    /// - TRANSFER_OUT: Transfer out from warehouse (-qty affects OnHand)
    /// - TRANSFER_IN: Transfer in to warehouse (+qty affects OnHand)
    /// </summary>
    public string MovementType { get; set; } = null!;
    
    /// <summary>
    /// Quantity of movement.
    /// Positive for inbound/reserve, negative for outbound/release.
    /// Must never be zero.
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Unit cost at time of receipt (optional, for costing in future)
    /// </summary>
    public decimal? UnitCost { get; set; }
    
    /// <summary>
    /// Type of document that triggered this movement
    /// Examples: "PurchaseReceipt", "SalesOrder", "StockAdjustment", "Transfer"
    /// </summary>
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// ID of the document that triggered this movement
    /// </summary>
    public Guid? ReferenceId { get; set; }
    
    /// <summary>
    /// Correlation ID to group related movements
    /// (e.g., TRANSFER_OUT and TRANSFER_IN share same correlationId)
    /// </summary>
    public Guid? CorrelationId { get; set; }
    
    /// <summary>
    /// Optional notes about this movement
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Allowed movement types
/// </summary>
public static class StockMovementType
{
    public const string INBOUND = "INBOUND";
    public const string OUTBOUND = "OUTBOUND";
    public const string ADJUSTMENT = "ADJUSTMENT";
    public const string RESERVE = "RESERVE";
    public const string RELEASE = "RELEASE";
    public const string TRANSFER_OUT = "TRANSFER_OUT";
    public const string TRANSFER_IN = "TRANSFER_IN";
    
    public static readonly HashSet<string> All = new()
    {
        INBOUND, OUTBOUND, ADJUSTMENT, RESERVE, RELEASE, TRANSFER_OUT, TRANSFER_IN
    };
}
