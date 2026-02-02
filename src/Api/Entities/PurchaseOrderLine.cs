using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class PurchaseOrderLine : TenantEntity
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Qty { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? VatRate { get; set; }
    public decimal ReceivedQty { get; set; }
    public string? Note { get; set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
