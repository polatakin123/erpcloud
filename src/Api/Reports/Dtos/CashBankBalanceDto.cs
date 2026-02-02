namespace ErpCloud.Api.Reports.Dtos;

public class CashBankBalanceDto
{
    public string SourceType { get; set; } = string.Empty; // CASHBOX or BANK
    public Guid SourceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Balance { get; set; }
}
