namespace ErpCloud.Api.Reports.Dtos;

public class StockBalanceDto
{
    public Guid VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal OnHand { get; set; }
    public decimal Reserved { get; set; }
    public decimal Available { get; set; }
}
