using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpCloud.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Adds RabbitMQ messaging services to the service collection
    /// </summary>
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddSingleton(options);
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }
}
