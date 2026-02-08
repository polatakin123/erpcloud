using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class GoodsReceiptLine : TenantEntity
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Qty { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Note { get; set; }

    /// <summary>
    /// Quantity returned to supplier via purchase returns
    /// </summary>
    public decimal ReturnedQty { get; set; } = 0;

    /// <summary>
    /// Remaining quantity available for return (Qty - ReturnedQty)
    /// </summary>
    public decimal RemainingQty => Qty - ReturnedQty;

    // Navigation properties
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
    public ICollection<PurchaseReturnLine> PurchaseReturnLines { get; set; } = new List<PurchaseReturnLine>();
}
