using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Demo event log for testing idempotency
/// </summary>
public class DemoEventLog : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }  // From OutboxMessage
    public string Payload { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
