using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class CreditNote : TenantEntity
{
    public Guid Id { get; set; }
    public string CreditNoteNo { get; set; } = string.Empty;
    public string Type { get; set; } = "SALES"; // SALES | PURCHASE
    public Guid SourceInvoiceId { get; set; }
    public Guid PartyId { get; set; }
    public DateTime IssueDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "DRAFT"; // DRAFT | ISSUED | CANCELLED
    public string? Note { get; set; }

    // Tracking
    public decimal AppliedAmount { get; set; } = 0;
    public decimal RemainingAmount { get; set; }

    // Navigation properties
    public Invoice? SourceInvoice { get; set; }
    public Party? Party { get; set; }
    public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();
}

public class CreditNoteLine
{
    public Guid Id { get; set; }
    public Guid CreditNoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? VariantId { get; set; }
    public decimal? Qty { get; set; }

    // Navigation properties
    public CreditNote? CreditNote { get; set; }
    public ProductVariant? Variant { get; set; }
}
