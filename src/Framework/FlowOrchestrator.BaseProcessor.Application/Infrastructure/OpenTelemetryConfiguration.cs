using FlowOrchestrator.BaseProcessor.Application.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FlowOrchestrator.BaseProcessor.Application.Infrastructure;

/// <summary>
/// Extension methods for configuring OpenTelemetry observability
/// Follows the same patterns as EntitiesManager.Api for architectural consistency
/// </summary>
public static class OpenTelemetryConfiguration
{
    /// <summary>
    /// Adds OpenTelemetry observability with tracing, metrics, and logging
    /// Uses the same configuration pattern as EntitiesManager.Api
    /// </summary>
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Use the same service name and version pattern as EntitiesManager.Api
        var serviceName = "BaseProcessorApplication";
        var serviceVersion = "1.0.0";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.instance.id"] = Environment.MachineName,
                ["service.namespace"] = "FlowOrchestrator"
            });

        // Add OpenTelemetry with the same pattern as EntitiesManager.Api
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation()
                    .AddSource("BaseProcessorApplication.*")
                    .AddSource("MassTransit")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("BaseProcessorApplication.*")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                    });
            })
            .WithLogging(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                    });

                // Only add console exporter in development when collector is not available
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                var useConsoleForDev = configuration.GetValue<bool>("OpenTelemetry:UseConsoleInDevelopment", true);

                if (isDevelopment && useConsoleForDev)
                {
                    builder.AddConsoleExporter();
                }
            });

        return services;
    }
}
