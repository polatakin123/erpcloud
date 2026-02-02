using Microsoft.Extensions.Hosting;

namespace ErpCloud.Api.Services;

/// <summary>
/// Hosted service wrapper for DemoEventConsumer
/// </summary>
public class DemoEventConsumerHostedService : BackgroundService
{
    private readonly DemoEventConsumer _consumer;

    public DemoEventConsumerHostedService(DemoEventConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.StartConsuming();
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer.StopConsuming();
        return base.StopAsync(cancellationToken);
    }
}
