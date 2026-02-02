namespace ErpCloud.Api.Models;

// ============================================================================
// STOCK BALANCE DTOs
// ============================================================================

public record StockBalanceDto(
    Guid WarehouseId,
    Guid VariantId,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    DateTime UpdatedAt
);

// ============================================================================
// STOCK LEDGER DTOs
// ============================================================================

public record StockLedgerDto(
    Guid Id,
    DateTime OccurredAt,
    Guid WarehouseId,
    Guid VariantId,
    string MovementType,
    decimal Quantity,
    decimal? UnitCost,
    string? ReferenceType,
    Guid? ReferenceId,
    Guid? CorrelationId,
    string? Note,
    DateTime CreatedAt,
    Guid CreatedBy
);

// ============================================================================
// STOCK OPERATION DTOs
// ============================================================================

public record ReceiveStockDto(
    Guid WarehouseId,
    Guid VariantId,
    decimal Qty,
    decimal? UnitCost,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note
);

public record IssueStockDto(
    Guid WarehouseId,
    Guid VariantId,
    decimal Qty,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note
);

public record ReserveStockDto(
    Guid WarehouseId,
    Guid VariantId,
    decimal Qty,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note
);

public record ReleaseReservationDto(
    Guid WarehouseId,
    Guid VariantId,
    decimal Qty,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note
);

public record TransferStockDto(
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    Guid VariantId,
    decimal Qty,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note
);

public record AdjustStockDto(
    Guid WarehouseId,
    Guid VariantId,
    decimal Qty,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note
);
