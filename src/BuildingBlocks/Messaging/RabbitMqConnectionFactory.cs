using RabbitMQ.Client;

namespace ErpCloud.BuildingBlocks.Messaging;

/// <summary>
/// RabbitMQ connection factory and channel management
/// </summary>
public interface IRabbitMqConnectionFactory
{
    IConnection GetConnection();
    IModel CreateChannel();
}

public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory, IDisposable
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMqConnectionFactory(RabbitMqOptions options)
    {
        _options = options;
    }

    public IConnection GetConnection()
    {
        if (_connection != null && _connection.IsOpen)
        {
            return _connection;
        }

        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.Username,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection();
            return _connection;
        }
    }

    public IModel CreateChannel()
    {
        var connection = GetConnection();
        var channel = connection.CreateModel();

        // Declare exchange
        channel.ExchangeDeclare(
            exchange: _options.Exchange,
            type: _options.ExchangeType,
            durable: true,
            autoDelete: false);

        return channel;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
