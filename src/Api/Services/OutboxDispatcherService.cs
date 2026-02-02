using ErpCloud.Api.Data;
using ErpCloud.BuildingBlocks.Messaging;
using ErpCloud.BuildingBlocks.Outbox;
using ErpCloud.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ErpCloud.Api.Services;

/// <summary>
/// Background service that polls outbox messages and publishes them to RabbitMQ
/// </summary>
public class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcherService> _logger;
    private readonly IEventPublisher _eventPublisher;
    
    private const int PollingIntervalSeconds = 2;
    private const int BatchSize = 50;
    private const int MaxAttempts = 10;
    private const int MaxBackoffSeconds = 60;

    public OutboxDispatcherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcherService> logger,
        IEventPublisher eventPublisher)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("OutboxDispatcher stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        // SELECT FOR UPDATE SKIP LOCKED to avoid concurrent processing
        var pendingMessages = await dbContext.Database
            .SqlQueryRaw<OutboxMessage>(@"
                SELECT * FROM outbox_messages
                WHERE status = 0 
                  AND (next_attempt_at IS NULL OR next_attempt_at <= NOW())
                ORDER BY occurred_at
                LIMIT {0}
                FOR UPDATE SKIP LOCKED
            ", BatchSize)
            .ToListAsync(cancellationToken);

        if (!pendingMessages.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            await ProcessMessageAsync(dbContext, message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ErpDbContext dbContext, OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Publish to RabbitMQ
            var eventMessage = new EventMessage
            {
                MessageId = message.Id,
                TenantId = message.TenantId,
                EventType = message.Type,
                Payload = message.Payload,
                Timestamp = message.OccurredAt
            };

            await _eventPublisher.PublishAsync(eventMessage, cancellationToken);

            // Mark as sent
            message.Status = OutboxMessageStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            
            _logger.LogInformation("Published message {MessageId} of type {Type}", message.Id, message.Type);
        }
        catch (Exception ex)
        {
            // Increment attempts and schedule retry with exponential backoff
            message.Attempts++;
            message.LastError = ex.Message;

            if (message.Attempts >= MaxAttempts)
            {
                message.Status = OutboxMessageStatus.Failed;
                _logger.LogError(ex, "Message {MessageId} failed after {Attempts} attempts", message.Id, message.Attempts);
            }
            else
            {
                // Exponential backoff: 2^attempts seconds, capped at MaxBackoffSeconds
                var backoffSeconds = Math.Min(Math.Pow(2, message.Attempts), MaxBackoffSeconds);
                message.NextAttemptAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
                
                _logger.LogWarning(ex, "Message {MessageId} failed (attempt {Attempts}/{MaxAttempts}), retry at {NextAttempt}",
                    message.Id, message.Attempts, MaxAttempts, message.NextAttemptAt);
            }
        }
    }
}
