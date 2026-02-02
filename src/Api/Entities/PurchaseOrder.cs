using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class PurchaseOrder : TenantEntity
{
    public Guid Id { get; set; }
    public string PoNo { get; set; } = null!;
    public Guid PartyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Status { get; set; } = null!; // DRAFT | CONFIRMED | COMPLETED | CANCELLED
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? Note { get; set; }

    // Navigation properties
    public Party Party { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
