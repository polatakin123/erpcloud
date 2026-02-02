namespace ErpCloud.Api.Models;

public record ShipmentDto(
    Guid Id,
    string ShipmentNo,
    Guid SalesOrderId,
    Guid BranchId,
    Guid WarehouseId,
    DateTime ShipmentDate,
    string Status,
    string? Note,
    List<ShipmentLineDto> Lines,
    DateTime CreatedAt,
    Guid CreatedBy
);

public record ShipmentLineDto(
    Guid Id,
    Guid ShipmentId,
    Guid SalesOrderLineId,
    Guid VariantId,
    decimal Qty,
    string? Note
);
