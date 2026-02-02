namespace ErpCloud.Api.Reports.Dtos;

public class PagedReportResult<T>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}
