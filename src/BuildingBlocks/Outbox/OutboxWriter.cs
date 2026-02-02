using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.BuildingBlocks.Outbox;

/// <summary>
/// Implementation of outbox writer
/// </summary>
public class OutboxWriter : IOutboxWriter
{
    private readonly DbContext _dbContext;

    public OutboxWriter(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<OutboxMessage>().AddAsync(message, cancellationToken);
    }

    public async Task AddEventAsync<TEvent>(Guid tenantId, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var message = new OutboxMessage
        {
            TenantId = tenantId,
            Type = @event.GetType().Name,
            Payload = JsonSerializer.Serialize(@event),
            OccurredAt = DateTime.UtcNow
        };

        await AddAsync(message, cancellationToken);
    }
}
