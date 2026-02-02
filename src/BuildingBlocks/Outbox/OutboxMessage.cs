namespace ErpCloud.BuildingBlocks.Outbox;

/// <summary>
/// Outbox message status
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

/// <summary>
/// Outbox message entity for transactional outbox pattern
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int Attempts { get; set; } = 0;
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
}
