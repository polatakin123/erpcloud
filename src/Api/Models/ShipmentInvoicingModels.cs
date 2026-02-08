using System.ComponentModel.DataAnnotations;

namespace ErpCloud.Api.Models;

/// <summary>
/// Request to create invoice from shipment line
/// </summary>
public record ShipmentInvoiceLineRequestDto(
    [Required] Guid ShipmentLineId,
    [Required, Range(0.001, double.MaxValue)] decimal Qty
);

/// <summary>
/// Request to create invoice from shipment
/// </summary>
public record CreateInvoiceFromShipmentDto(
    string? InvoiceNo, // Optional - will be auto-generated if null
    DateTime? IssueDate,
    DateTime? DueDate,
    string? Note,
    List<ShipmentInvoiceLineRequestDto>? Lines // null = invoice all remaining qty
);

/// <summary>
/// Preview response for shipment-based invoice
/// </summary>
public record ShipmentInvoicePreviewDto(
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    List<ShipmentInvoiceLinePreviewDto> Lines
);

/// <summary>
/// Preview line for shipment-based invoice
/// </summary>
public record ShipmentInvoiceLinePreviewDto(
    Guid ShipmentLineId,
    Guid SalesOrderLineId,
    Guid VariantId,
    string VariantName,
    decimal Qty,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    decimal VatAmount,
    string Description
);

/// <summary>
/// Enhanced invoice DTO with source information
/// </summary>
public record InvoiceWithSourceDto(
    Guid Id,
    string InvoiceNo,
    string Type,
    Guid PartyId,
    string PartyName,
    Guid BranchId,
    DateTime IssueDate,
    DateTime? DueDate,
    string Currency,
    string Status,
    string? SourceType,
    Guid? SourceId,
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    string? Note,
    List<InvoiceLineWithSourceDto> Lines,
    DateTime CreatedAt,
    Guid CreatedBy
);

/// <summary>
/// Enhanced invoice line DTO with source information
/// </summary>
public record InvoiceLineWithSourceDto(
    Guid Id,
    Guid InvoiceId,
    Guid? ShipmentLineId,
    Guid? SalesOrderLineId,
    Guid? VariantId,
    string Description,
    decimal? Qty,
    decimal? UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    decimal VatAmount
);

/// <summary>
/// Shipment invoicing status
/// </summary>
public record ShipmentInvoicingStatusDto(
    bool IsFullyInvoiced,
    decimal TotalQty,
    decimal InvoicedQty,
    decimal RemainingQty,
    List<ShipmentLineInvoicingStatusDto> Lines
);

/// <summary>
/// Shipment line invoicing status
/// </summary>
public record ShipmentLineInvoicingStatusDto(
    Guid ShipmentLineId,
    Guid VariantId,
    string VariantName,
    decimal Qty,
    decimal InvoicedQty,
    decimal RemainingQty
);
