using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class EDocument : TenantEntity
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string DocumentType { get; set; } = null!; // EARCHIVE | EINVOICE
    public string Scenario { get; set; } = null!; // BASIC | COMMERCIAL
    public string Status { get; set; } = null!; // DRAFT | QUEUED | SENDING | SENT | ACCEPTED | REJECTED | CANCELLED | ERROR
    public string ProviderCode { get; set; } = null!; // TEST, NILVERA, UYUMSOFT, etc.
    public Guid Uuid { get; set; } // e-belge uuid
    public string? EnvelopeId { get; set; }
    public string? GIBReference { get; set; }
    public string? LastStatusMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastTriedAt { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
    public ICollection<EDocumentStatusHistory> StatusHistory { get; set; } = new List<EDocumentStatusHistory>();
}
