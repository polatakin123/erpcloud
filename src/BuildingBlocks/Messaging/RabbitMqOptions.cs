namespace ErpCloud.BuildingBlocks.Messaging;

/// <summary>
/// RabbitMQ connection configuration
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "erp.events";
    public string ExchangeType { get; set; } = "topic";
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
