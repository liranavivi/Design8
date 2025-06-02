using FlowOrchestrator.BaseProcessor.Application.Consumers;
using FlowOrchestrator.BaseProcessor.Application.Models;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowOrchestrator.BaseProcessor.Application.Infrastructure;

/// <summary>
/// Extension methods for configuring MassTransit with RabbitMQ
/// Following EntitiesManager.Api patterns for consistency
/// </summary>
public static class MassTransitConfiguration
{
    /// <summary>
    /// Adds MassTransit with RabbitMQ transport and consumers
    /// Following EntitiesManager.Api configuration patterns
    /// </summary>
    public static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>()
            ?? new RabbitMQConfiguration();

        services.AddMassTransit(x =>
        {
            // Add consumers following EntitiesManager.Api patterns
            x.AddConsumer<ExecuteActivityCommandConsumer>();
            x.AddConsumer<GetHealthStatusCommandConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMqConfig.Username);
                    h.Password(rabbitMqConfig.Password);
                });

                // Configure retry policy following EntitiesManager.Api patterns
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));

                // Configure receive endpoints following EntitiesManager.Api patterns
                cfg.ReceiveEndpoint("base-processor-execute-activity", e =>
                {
                    e.ConfigureConsumer<ExecuteActivityCommandConsumer>(context);
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30)));
                    
                    // Set concurrency limit
                    e.ConcurrentMessageLimit = rabbitMqConfig.ConcurrencyLimit;
                    e.PrefetchCount = rabbitMqConfig.PrefetchCount;
                });

                cfg.ReceiveEndpoint("base-processor-health", e =>
                {
                    e.ConfigureConsumer<GetHealthStatusCommandConsumer>(context);
                    e.PrefetchCount = rabbitMqConfig.PrefetchCount;
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
