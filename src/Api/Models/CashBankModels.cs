using System.ComponentModel.DataAnnotations;

namespace ErpCloud.Api.Models;

// ==================== Cashbox DTOs ====================

public record CreateCashboxDto
{
    [Required, StringLength(32, MinimumLength = 2), RegularExpression(@"^[A-Z0-9_-]+$")]
    public string Code { get; set; } = string.Empty;
    
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; } = "TRY";
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public record UpdateCashboxDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; } = "TRY";
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public record CashboxDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public record CashboxListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}

public record CashboxSearchDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 50;
    public string? Q { get; set; }
    public bool? Active { get; set; }
}

// ==================== BankAccount DTOs ====================

public record CreateBankAccountDto
{
    [Required, StringLength(32, MinimumLength = 2), RegularExpression(@"^[A-Z0-9_-]+$")]
    public string Code { get; set; } = string.Empty;
    
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? BankName { get; set; }
    
    [StringLength(34, MinimumLength = 15), RegularExpression(@"^[A-Z0-9]+$")]
    public string? Iban { get; set; }
    
    [Required, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; } = "TRY";
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public record UpdateBankAccountDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? BankName { get; set; }
    
    [StringLength(34, MinimumLength = 15), RegularExpression(@"^[A-Z0-9]+$")]
    public string? Iban { get; set; }
    
    [Required, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; } = "TRY";
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public record BankAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public record BankAccountListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}

public record BankAccountSearchDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 50;
    public string? Q { get; set; }
    public bool? Active { get; set; }
}

// ==================== CashBankLedger DTOs ====================

public record CashBankLedgerDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PaymentNo { get; set; }
    public string? Description { get; set; }
    public decimal AmountSigned { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public record CashBankLedgerSearchDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 50;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record CashBankBalanceDto
{
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public record CashBankBalanceQueryDto
{
    [Required]
    public string SourceType { get; set; } = string.Empty;
    
    [Required]
    public Guid SourceId { get; set; }
    
    public DateTime? At { get; set; }
}
