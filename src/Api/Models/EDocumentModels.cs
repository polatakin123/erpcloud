namespace ErpCloud.Api.Models;

// DTOs
public record CreateEDocumentDto(
    Guid InvoiceId,
    string DocumentType, // EARCHIVE | EINVOICE
    string? Scenario = "BASIC" // BASIC | COMMERCIAL
);

public record EDocumentDto(
    Guid Id,
    Guid TenantId,
    Guid InvoiceId,
    string DocumentType,
    string Scenario,
    string Status,
    string ProviderCode,
    Guid Uuid,
    string? EnvelopeId,
    string? GIBReference,
    string? LastStatusMessage,
    int RetryCount,
    DateTime? LastTriedAt,
    DateTime CreatedAt,
    Guid CreatedBy
);

public record EDocumentStatusHistoryDto(
    Guid Id,
    Guid EDocumentId,
    string Status,
    string? Message,
    DateTime OccurredAt
);

public record EDocumentWithHistoryDto(
    Guid Id,
    Guid TenantId,
    Guid InvoiceId,
    string DocumentType,
    string Scenario,
    string Status,
    string ProviderCode,
    Guid Uuid,
    string? EnvelopeId,
    string? GIBReference,
    string? LastStatusMessage,
    int RetryCount,
    DateTime? LastTriedAt,
    DateTime CreatedAt,
    Guid CreatedBy,
    List<EDocumentStatusHistoryDto> StatusHistory
);

// Query models
public record EDocumentQuery(
    Guid? InvoiceId = null,
    string? Status = null,
    string? DocumentType = null,
    int Page = 1,
    int PageSize = 50
);

// Result models
public record SendResult(
    bool Success,
    string Status, // SENT | ERROR
    string? Message = null,
    string? EnvelopeId = null
);

public record StatusResult(
    string Status, // ACCEPTED | REJECTED | PENDING
    string? Message = null,
    string? GIBReference = null
);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
