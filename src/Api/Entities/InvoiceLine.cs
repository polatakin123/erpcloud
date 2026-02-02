using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Invoice line item
/// </summary>
public class InvoiceLine : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK to Invoice
    /// </summary>
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    /// <summary>
    /// Optional shipment line reference (FK to ShipmentLine)
    /// </summary>
    public Guid? ShipmentLineId { get; set; }
    public ShipmentLine? ShipmentLine { get; set; }

    /// <summary>
    /// Optional sales order line reference (FK to SalesOrderLine)
    /// </summary>
    public Guid? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }

    /// <summary>
    /// Optional product variant (FK to ProductVariant)
    /// Can be null for description-only lines
    /// </summary>
    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    /// <summary>
    /// Line description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity (optional for service/description lines)
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// Unit price (optional for manual lines)
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// VAT rate (percentage)
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Line total (excluding VAT)
    /// Can be calculated (Qty * UnitPrice) or manually entered
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// VAT amount for this line
    /// </summary>
    public decimal VatAmount { get; set; }
}
