using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class PurchaseReturn : TenantEntity
{
    public Guid Id { get; set; }
    public string PurchaseReturnNo { get; set; } = string.Empty;
    public Guid GoodsReceiptId { get; set; }
    public Guid PartyId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Status { get; set; } = "DRAFT"; // DRAFT | SHIPPED | CANCELLED
    public DateTime ReturnDate { get; set; }
    public string? Note { get; set; }

    // Navigation properties
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Party? Party { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<PurchaseReturnLine> Lines { get; set; } = new List<PurchaseReturnLine>();
}

public class PurchaseReturnLine
{
    public Guid Id { get; set; }
    public Guid PurchaseReturnId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Qty { get; set; }
    public string? ReasonCode { get; set; }

    // Navigation properties
    public PurchaseReturn? PurchaseReturn { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public ProductVariant? Variant { get; set; }
}
