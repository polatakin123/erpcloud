using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Materialized stock balance for performance.
/// This is NOT the source of truth - it's derived from StockLedgerEntry.
/// Updated transactionally with each ledger entry.
/// </summary>
public class StockBalance : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Which warehouse this balance is for
    /// </summary>
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    /// <summary>
    /// Which product variant (SKU) this balance is for
    /// </summary>
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    
    /// <summary>
    /// Physical stock on hand
    /// Calculated from: INBOUND, OUTBOUND, ADJUSTMENT, TRANSFER_IN, TRANSFER_OUT
    /// </summary>
    public decimal OnHand { get; set; }
    
    /// <summary>
    /// Reserved stock (allocated but not yet issued)
    /// Calculated from: RESERVE, RELEASE
    /// </summary>
    public decimal Reserved { get; set; }
    
    /// <summary>
    /// Available = OnHand - Reserved
    /// This is calculated, not stored
    /// </summary>
    public decimal Available => OnHand - Reserved;
    
    /// <summary>
    /// When this balance was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
