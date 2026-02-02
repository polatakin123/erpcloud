using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ErpCloud.BuildingBlocks.Messaging;

/// <summary>
/// Base class for RabbitMQ event consumers with idempotency support
/// </summary>
public abstract class RabbitMqConsumerBase
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;
    private IModel? _channel;

    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    protected RabbitMqConsumerBase(
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqOptions options,
        ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public void StartConsuming()
    {
        _channel = _connectionFactory.CreateChannel();

        // Declare queue
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind queue to exchange with routing keys
        foreach (var routingKey in RoutingKeys)
        {
            _channel.QueueBind(
                queue: QueueName,
                exchange: _options.Exchange,
                routingKey: routingKey);
        }

        // Set QoS - process one message at a time
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var success = false;
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<EventMessage>(messageJson);

                if (message != null)
                {
                    // Process message (idempotency check inside)
                    await ProcessMessageAsync(message);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", QueueName);
            }
            finally
            {
                if (success)
                {
                    // Manual ACK
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Message acknowledged from queue {QueueName}", QueueName);
                }
                else
                {
                    // NACK and requeue (or send to dead letter)
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    _logger.LogWarning("Message rejected from queue {QueueName}", QueueName);
                }
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Started consuming from queue {QueueName}", QueueName);
    }

    protected abstract Task ProcessMessageAsync(EventMessage message);

    public void StopConsuming()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}
