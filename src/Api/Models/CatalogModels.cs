namespace ErpCloud.Api.Models;

// Product DTOs
public record CreateProductDto(
    string Code,
    string Name,
    string? Description,
    bool IsActive
);

public record UpdateProductDto(
    string Code,
    string Name,
    string? Description,
    bool IsActive
);

public record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    Guid CreatedBy
);

// ProductVariant DTOs
public record CreateProductVariantDto(
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal VatRate,
    bool IsActive
);

public record UpdateProductVariantDto(
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal VatRate,
    bool IsActive
);

public record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal VatRate,
    bool IsActive,
    DateTime CreatedAt,
    Guid CreatedBy
);

// PriceList DTOs
public record CreatePriceListDto(
    string Code,
    string Name,
    string Currency,
    bool IsDefault
);

public record UpdatePriceListDto(
    string Code,
    string Name,
    string Currency,
    bool IsDefault
);

public record PriceListDto(
    Guid Id,
    string Code,
    string Name,
    string Currency,
    bool IsDefault,
    DateTime CreatedAt,
    Guid CreatedBy
);

// PriceListItem DTOs
public record CreatePriceListItemDto(
    Guid VariantId,
    decimal UnitPrice,
    decimal? MinQty,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

public record UpdatePriceListItemDto(
    Guid VariantId,
    decimal UnitPrice,
    decimal? MinQty,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

public record PriceListItemDto(
    Guid Id,
    Guid PriceListId,
    Guid VariantId,
    decimal UnitPrice,
    decimal? MinQty,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    DateTime CreatedAt,
    Guid CreatedBy
);

// Pricing Query Result
public record VariantPriceDto(
    Guid VariantId,
    string Sku,
    string VariantName,
    string PriceListCode,
    string Currency,
    decimal UnitPrice,
    decimal VatRate,
    decimal? MinQty,
    DateTime? ValidFrom,
    DateTime? ValidTo
);
