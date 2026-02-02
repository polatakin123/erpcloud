using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Shipment (irsaliye/sevkiyat) entity.
/// Represents physical stock issue from warehouse based on confirmed sales orders.
/// </summary>
public class Shipment : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique shipment number within tenant (e.g., "SHIP-2024-001")
    /// </summary>
    public string ShipmentNo { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the sales order being fulfilled
    /// </summary>
    public Guid SalesOrderId { get; set; }
    
    /// <summary>
    /// Branch issuing the shipment
    /// </summary>
    public Guid BranchId { get; set; }
    
    /// <summary>
    /// Warehouse from which stock is issued
    /// </summary>
    public Guid WarehouseId { get; set; }
    
    /// <summary>
    /// Date of shipment
    /// </summary>
    public DateTime ShipmentDate { get; set; }
    
    /// <summary>
    /// Shipment status: DRAFT | SHIPPED | CANCELLED
    /// </summary>
    public string Status { get; set; } = "DRAFT";
    
    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Note { get; set; }
    
    // Navigation properties
    public SalesOrder? SalesOrder { get; set; }
    public Branch? Branch { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<ShipmentLine> Lines { get; set; } = new List<ShipmentLine>();
}
