using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ErpCloud.BuildingBlocks.Messaging;

/// <summary>
/// Domain event message
/// </summary>
public class EventMessage
{
    public Guid MessageId { get; set; }
    public Guid TenantId { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// RabbitMQ event publisher
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(EventMessage message, CancellationToken cancellationToken = default);
}

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqOptions options,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public Task PublishAsync(EventMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = _connectionFactory.CreateChannel();

            // Routing key: {tenantId}.{eventType}
            var routingKey = $"{message.TenantId}.{message.EventType}";

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = message.MessageId.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.ContentType = "application/json";
            properties.Headers = new Dictionary<string, object>
            {
                ["tenant_id"] = message.TenantId.ToString(),
                ["event_type"] = message.EventType
            };

            channel.BasicPublish(
                exchange: _options.Exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published event {EventType} for tenant {TenantId} with message ID {MessageId}",
                message.EventType, message.TenantId, message.MessageId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType} for tenant {TenantId}",
                message.EventType, message.TenantId);
            throw;
        }
    }
}
