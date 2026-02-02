namespace ErpCloud.Api.Reports.Dtos;

public class SalesSummaryDto
{
    public string Period { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }
}
