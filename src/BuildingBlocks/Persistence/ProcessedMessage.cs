namespace ErpCloud.BuildingBlocks.Persistence;

/// <summary>
/// Entity for tracking processed messages (idempotency)
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
