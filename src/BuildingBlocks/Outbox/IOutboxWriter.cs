namespace ErpCloud.BuildingBlocks.Outbox;

/// <summary>
/// Service for writing messages to the outbox
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Adds a message to the outbox (within the current transaction)
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a domain event to the outbox
    /// </summary>
    Task AddEventAsync<TEvent>(Guid tenantId, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
