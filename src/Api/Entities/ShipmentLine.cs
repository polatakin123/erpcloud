using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Shipment line item representing quantity shipped for a sales order line
/// </summary>
public class ShipmentLine : TenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Parent shipment
    /// </summary>
    public Guid ShipmentId { get; set; }
    
    /// <summary>
    /// Reference to the sales order line being fulfilled
    /// </summary>
    public Guid SalesOrderLineId { get; set; }
    
    /// <summary>
    /// Product variant being shipped
    /// </summary>
    public Guid VariantId { get; set; }
    
    /// <summary>
    /// Quantity shipped (must be > 0 and <= reserved qty)
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// Quantity invoiced from this shipment line (0 <= InvoicedQty <= Qty)
    /// </summary>
    public decimal InvoicedQty { get; set; }
    
    /// <summary>
    /// Optional line notes
    /// </summary>
    public string? Note { get; set; }
    
    // Navigation properties
    public Shipment? Shipment { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }
    public ProductVariant? Variant { get; set; }
}
