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

    // Navigation properties
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
