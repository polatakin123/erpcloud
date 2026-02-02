using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Sales Order line item
/// </summary>
public class SalesOrderLine : TenantEntity
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// FK to SalesOrder
    /// </summary>
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>
    /// Product variant (FK to ProductVariant)
    /// </summary>
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    /// <summary>
    /// Ordered quantity
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// Unit price (excluding VAT)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// VAT rate (percentage)
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Line total (Qty * UnitPrice, excluding VAT)
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Reserved quantity for this line (for tracking, source of truth is ledger)
    /// </summary>
    public decimal ReservedQty { get; set; }

    /// <summary>
    /// Quantity shipped (sum of all shipment lines for this order line)
    /// </summary>
    public decimal ShippedQty { get; set; }

    /// <summary>
    /// Optional line note
    /// </summary>
    public string? Note { get; set; }
}
