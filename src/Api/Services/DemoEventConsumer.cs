using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Events;
using ErpCloud.BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ErpCloud.Api.Services;

/// <summary>
/// Consumer for DemoEventCreated events
/// </summary>
public class DemoEventConsumer : IdempotentConsumerBase
{
    protected override string QueueNameValue => "erp.demo.events";
    protected override string[] RoutingKeysValue => new[] { "*.DemoEventCreated" };

    public DemoEventConsumer(
        IServiceProvider serviceProvider,
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqOptions options,
        ILogger<DemoEventConsumer> logger)
        : base(serviceProvider, connectionFactory, options, logger)
    {
    }

    protected override Task HandleEventAsync(ErpDbContext dbContext, EventMessage eventMessage)
    {
        var demoEvent = JsonSerializer.Deserialize<DemoEventCreated>(eventMessage.Payload);
        
        if (demoEvent == null)
        {
            throw new InvalidOperationException("Failed to deserialize DemoEventCreated");
        }

        // Log to console
        Console.WriteLine($"[DEMO] Tenant={eventMessage.TenantId}, OrderNo={demoEvent.OrderNo}, Amount={demoEvent.Amount}");

        // Insert to demo_event_logs table
        var logEntry = new DemoEventLog
        {
            Id = Guid.NewGuid(),
            TenantId = eventMessage.TenantId,
            MessageId = eventMessage.MessageId,
            Payload = eventMessage.Payload,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty  // System
        };

        dbContext.DemoEventLogs.Add(logEntry);
        
        return Task.CompletedTask;
    }
}
