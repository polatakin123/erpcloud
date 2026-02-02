namespace ErpCloud.Api.Reports.Dtos;

public class PartyBalanceDto
{
    public Guid PartyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "TRY";
}
