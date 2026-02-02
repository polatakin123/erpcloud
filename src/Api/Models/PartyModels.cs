namespace ErpCloud.Api.Models;

// Party DTOs
public record CreatePartyDto(
    string Code,
    string Name,
    string Type,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    decimal? CreditLimit,
    int? PaymentTermDays,
    bool IsActive
);

public record UpdatePartyDto(
    string Code,
    string Name,
    string Type,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    decimal? CreditLimit,
    int? PaymentTermDays,
    bool IsActive
);

public record PartyDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    decimal? CreditLimit,
    int? PaymentTermDays,
    bool IsActive,
    DateTime CreatedAt,
    Guid CreatedBy
);
