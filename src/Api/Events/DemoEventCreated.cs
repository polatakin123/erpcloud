namespace ErpCloud.Api.Events;

/// <summary>
/// Demo event for testing outbox pattern
/// </summary>
public class DemoEventCreated
{
    public string OrderNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
