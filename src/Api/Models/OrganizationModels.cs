namespace ErpCloud.Api.Models;

// Organization DTOs
public record CreateOrganizationDto(
    string Code,
    string Name,
    string? TaxNumber
);

public record UpdateOrganizationDto(
    string Code,
    string Name,
    string? TaxNumber
);

public record OrganizationDto(
    Guid Id,
    string Code,
    string Name,
    string? TaxNumber,
    DateTime CreatedAt,
    Guid CreatedBy
);

// Branch DTOs
public record CreateBranchDto(
    string Code,
    string Name,
    string? City,
    string? Address
);

public record UpdateBranchDto(
    string Code,
    string Name,
    string? City,
    string? Address
);

public record BranchDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? City,
    string? Address,
    DateTime CreatedAt,
    Guid CreatedBy
);

// Warehouse DTOs
public record CreateWarehouseDto(
    string Code,
    string Name,
    string Type,
    bool IsDefault
);

public record UpdateWarehouseDto(
    string Code,
    string Name,
    string Type,
    bool IsDefault
);

public record WarehouseDto(
    Guid Id,
    Guid BranchId,
    string Code,
    string Name,
    string Type,
    bool IsDefault,
    DateTime CreatedAt,
    Guid CreatedBy
);

// Pagination response
public record PaginatedResponse<T>(
    int Page,
    int Size,
    int Total,
    List<T> Items
);
