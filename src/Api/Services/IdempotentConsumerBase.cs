using ErpCloud.Api.Data;
using ErpCloud.BuildingBlocks.Messaging;
using ErpCloud.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace ErpCloud.Api.Services;

/// <summary>
/// Base class for idempotent event consumers
/// </summary>
public abstract class IdempotentConsumerBase : RabbitMqConsumerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    protected abstract string QueueNameValue { get; }
    protected abstract string[] RoutingKeysValue { get; }
    
    protected override string QueueName => QueueNameValue;
    protected override string[] RoutingKeys => RoutingKeysValue;

    protected IdempotentConsumerBase(
        IServiceProvider serviceProvider,
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqOptions options,
        ILogger logger)
        : base(connectionFactory, options, logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ProcessMessageAsync(EventMessage eventMessage)
    {
        var messageId = eventMessage.MessageId;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Check if already processed (idempotency)
            var alreadyProcessed = await dbContext.Set<ProcessedMessage>()
                .AnyAsync(p => p.TenantId == eventMessage.TenantId && p.MessageId == messageId);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Message {MessageId} already processed, skipping", messageId);
                return;
            }

            // Mark as processed BEFORE handling (prevents duplicate if handler fails)
            var processedMessage = new ProcessedMessage
            {
                MessageId = messageId,
                TenantId = eventMessage.TenantId,
                ProcessedAt = DateTime.UtcNow
            };
            dbContext.Set<ProcessedMessage>().Add(processedMessage);
            await dbContext.SaveChangesAsync();

            // Handle the event
            await HandleEventAsync(dbContext, eventMessage);

            // Commit transaction
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully processed message {MessageId} of type {EventType}",
                messageId, eventMessage.EventType);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Unique constraint violation - message already processed by another consumer instance
            _logger.LogInformation("Message {MessageId} already processed (unique constraint), skipping", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", messageId);
            await transaction.RollbackAsync();
            throw; // Rethrow to trigger NACK
        }
    }

    /// <summary>
    /// Handle the event (to be implemented by derived classes)
    /// </summary>
    protected abstract Task HandleEventAsync(ErpDbContext dbContext, EventMessage eventMessage);
}
