using ErpCloud.Api.Data;
using ErpCloud.BuildingBlocks.Messaging;
using ErpCloud.BuildingBlocks.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Workers;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly IEventPublisher _eventPublisher;
    private const int BatchSize = 50;
    private const int MaxAttempts = 10;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherService> logger,
        IEventPublisher eventPublisher)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher service started");

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

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisher service stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        // Pending mesajları al (batch halinde)
        var pendingMessages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (!pendingMessages.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessSingleMessageAsync(message, dbContext, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSingleMessageAsync(
        OutboxMessage message,
        ErpDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // EventMessage oluştur
            var eventMessage = new EventMessage
            {
                MessageId = message.Id,
                TenantId = message.TenantId,
                EventType = message.Type,
                Payload = message.Payload,
                Timestamp = message.OccurredAt
            };

            // RabbitMQ'ya publish et
            await _eventPublisher.PublishAsync(eventMessage, cancellationToken);

            // Başarılı - durumu güncelle
            message.Status = OutboxMessageStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            message.LastError = null;

            _logger.LogInformation(
                "Published outbox message {MessageId} of type {EventType} for tenant {TenantId}",
                message.Id, message.Type, message.TenantId);
        }
        catch (Exception ex)
        {
            // Hata - attempts artır
            message.Attempts++;
            message.LastError = ex.Message.Length > 2000 
                ? ex.Message.Substring(0, 2000) 
                : ex.Message;

            if (message.Attempts >= MaxAttempts)
            {
                message.Status = OutboxMessageStatus.Failed;
                _logger.LogError(ex,
                    "Failed to publish outbox message {MessageId} after {Attempts} attempts. Marked as Failed",
                    message.Id, message.Attempts);
            }
            else
            {
                _logger.LogWarning(ex,
                    "Failed to publish outbox message {MessageId}. Attempt {Attempts}/{MaxAttempts}",
                    message.Id, message.Attempts, MaxAttempts);
            }
        }
    }
}
