using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class GoodsReceipt : TenantEntity
{
    public Guid Id { get; set; }
    public string GrnNo { get; set; } = null!;
    public Guid PurchaseOrderId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateOnly ReceiptDate { get; set; }
    public string Status { get; set; } = null!; // DRAFT | RECEIVED | CANCELLED
    public string? Note { get; set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
    public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
}
