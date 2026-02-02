namespace ErpCloud.Api.Models;

// ================== INVOICE DTOS ==================

public record InvoiceDto(
    Guid Id,
    string InvoiceNo,
    string Type,
    Guid PartyId,
    string PartyName,
    Guid BranchId,
    string BranchName,
    DateTime IssueDate,
    DateTime? DueDate,
    string Currency,
    string Status,
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    string? Note,
    List<InvoiceLineDto> Lines,
    DateTime CreatedAt,
    // Payment Allocation Fields
    decimal PaidAmount,
    decimal OpenAmount,
    string PaymentStatus  // OPEN | PARTIAL | PAID
);

public record InvoiceLineDto(
    Guid Id,
    Guid? VariantId,
    string? Sku,
    string Description,
    decimal? Qty,
    decimal? UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    decimal VatAmount
);

public record CreateInvoiceDto(
    string InvoiceNo,
    string Type,
    Guid PartyId,
    Guid BranchId,
    DateTime IssueDate,
    DateTime? DueDate,
    string? Currency,
    string? Note,
    List<CreateInvoiceLineDto> Lines
);

public record CreateInvoiceLineDto(
    Guid? VariantId,
    string Description,
    decimal? Qty,
    decimal? UnitPrice,
    decimal VatRate,
    decimal? LineTotal  // Optional: if null, calculated from Qty*UnitPrice
);

public record UpdateInvoiceDto(
    string InvoiceNo,
    string Type,
    Guid PartyId,
    Guid BranchId,
    DateTime IssueDate,
    DateTime? DueDate,
    string? Currency,
    string? Note,
    List<UpdateInvoiceLineDto> Lines
);

public record UpdateInvoiceLineDto(
    Guid? VariantId,
    string Description,
    decimal? Qty,
    decimal? UnitPrice,
    decimal VatRate,
    decimal? LineTotal
);

public record InvoiceSearchDto(
    int Page = 1,
    int Size = 50,
    string? Q = null,
    string? Type = null,
    string? Status = null,
    Guid? PartyId = null,
    DateTime? From = null,
    DateTime? To = null,
    string? PaymentStatus = null,  // OPEN | PARTIAL | PAID
    bool OpenOnly = false  // Filter invoices with OpenAmount > 0
);

public record InvoiceListDto(
    List<InvoiceDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// ================== PAYMENT DTOS ==================

public record PaymentDto(
    Guid Id,
    string PaymentNo,
    Guid PartyId,
    string PartyName,
    Guid BranchId,
    string BranchName,
    DateTime Date,
    string Direction,
    string Method,
    string Currency,
    decimal Amount,
    string? Note,
    string? SourceType,
    Guid? SourceId,
    DateTime CreatedAt,
    // Payment Allocation Fields
    decimal AllocatedAmount,
    decimal UnallocatedAmount
);

public record CreatePaymentDto(
    string PaymentNo,
    Guid PartyId,
    Guid BranchId,
    DateTime Date,
    string Direction,
    string Method,
    string? Currency,
    decimal Amount,
    string? Note,
    string? SourceType,
    Guid? SourceId
);

public record PaymentSearchDto(
    int Page = 1,
    int Size = 50,
    string? Q = null,
    Guid? PartyId = null,
    string? Direction = null,
    DateTime? From = null,
    DateTime? To = null,
    bool UnallocatedOnly = false  // Filter payments with UnallocatedAmount > 0
);

public record PaymentListDto(
    List<PaymentDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// ================== PARTY LEDGER DTOS ==================

public record PartyLedgerEntryDto(
    Guid Id,
    DateTime OccurredAt,
    string SourceType,
    Guid SourceId,
    string Description,
    decimal AmountSigned,
    string Currency,
    decimal? OpenAmountSigned
);

public record PartyLedgerListDto(
    List<PartyLedgerEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record PartyBalanceDto(
    decimal Balance,
    string Currency
);

public record PartyLedgerSearchDto(
    int Page = 1,
    int Size = 50,
    DateTime? From = null,
    DateTime? To = null
);
