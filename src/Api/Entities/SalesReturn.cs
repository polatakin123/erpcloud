using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class SalesReturn : TenantEntity
{
    public Guid Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public Guid SalesInvoiceId { get; set; }
    public Guid PartyId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Status { get; set; } = "DRAFT"; // DRAFT | RECEIVED | CANCELLED
    public DateTime ReturnDate { get; set; }
    public string? Note { get; set; }

    // Navigation properties
    public Invoice? Invoice { get; set; }
    public Party? Party { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<SalesReturnLine> Lines { get; set; } = new List<SalesReturnLine>();
}

public class SalesReturnLine
{
    public Guid Id { get; set; }
    public Guid SalesReturnId { get; set; }
    public Guid InvoiceLineId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Qty { get; set; }
    public string? ReasonCode { get; set; }

    // Navigation properties
    public SalesReturn? SalesReturn { get; set; }
    public InvoiceLine? InvoiceLine { get; set; }
    public ProductVariant? Variant { get; set; }
}
