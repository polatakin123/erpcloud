namespace ErpCloud.Api.Models;

// ================== RESPONSE DTOS ==================

public record SalesOrderDto(
    Guid Id,
    string OrderNo,
    Guid PartyId,
    string PartyName,
    Guid BranchId,
    string BranchName,
    Guid WarehouseId,
    string WarehouseName,
    Guid? PriceListId,
    string? PriceListCode,
    string Status,
    DateTime OrderDate,
    string? Note,
    List<SalesOrderLineDto> Lines,
    DateTime CreatedAt
);

public record SalesOrderLineDto(
    Guid Id,
    Guid VariantId,
    string Sku,
    string VariantName,
    decimal Qty,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    decimal ReservedQty,
    string? Note
);

// ================== REQUEST DTOS ==================

public record CreateSalesOrderDto(
    string OrderNo,
    Guid PartyId,
    Guid BranchId,
    Guid WarehouseId,
    Guid? PriceListId,
    DateTime OrderDate,
    string? Note,
    List<CreateSalesOrderLineDto> Lines
);

public record CreateSalesOrderLineDto(
    Guid VariantId,
    decimal Qty,
    decimal? UnitPrice,  // Optional, if null fetch from pricing
    decimal? VatRate,    // Optional, if null fetch from variant
    string? Note
);

public record UpdateSalesOrderDto(
    string OrderNo,
    Guid PartyId,
    Guid BranchId,
    Guid WarehouseId,
    Guid? PriceListId,
    DateTime OrderDate,
    string? Note,
    List<UpdateSalesOrderLineDto> Lines
);

public record UpdateSalesOrderLineDto(
    Guid VariantId,
    decimal Qty,
    decimal? UnitPrice,
    decimal? VatRate,
    string? Note
);

// ================== SEARCH/FILTER ==================

public record SalesOrderSearchDto(
    int Page = 1,
    int Size = 50,
    string? Q = null,
    string? Status = null,
    Guid? PartyId = null
);

public record SalesOrderListDto(
    List<SalesOrderDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
