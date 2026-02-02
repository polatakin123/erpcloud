namespace ErpCloud.Api.Reports.Dtos;

public class StockMovementDto
{
    public DateTime OccurredAt { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string? Note { get; set; }
}
