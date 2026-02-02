namespace ErpCloud.Api.Reports.Dtos;

public class PartyAgingDto
{
    public Guid PartyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Bucket0_30 { get; set; }
    public decimal Bucket31_60 { get; set; }
    public decimal Bucket61_90 { get; set; }
    public decimal Bucket90Plus { get; set; }
    public decimal Total { get; set; }
}
