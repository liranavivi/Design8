using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using FlowOrchestrator.EntitiesManagers.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowOrchestrator.BaseProcessor.Application.Examples;

/// <summary>
/// Sample concrete implementation of BaseProcessorApplication
/// Demonstrates how to create a specific processor service
/// The base class now provides a complete default implementation that can be overridden if needed
/// </summary>
public class SampleProcessorApplication : BaseProcessorApplication
{
    // Main entry point remains the same
    public static async Task<int> Main(string[] args)
    {
        var app = new SampleProcessorApplication();
        return await app.RunAsync(args);
    }

    /// <summary>
    /// Concrete implementation of the activity processing logic
    /// This is where the specific processor business logic is implemented
    /// </summary>
    protected override async Task<ProcessedActivityData> ProcessActivityDataAsync(
        Guid processorId,
        Guid orchestratedFlowEntityId,
        Guid stepId,
        Guid executionId,
        List<BaseEntity> entities,
        JsonElement inputData,
        JsonElement? inputMetadata,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Get logger from service provider
        var logger = ServiceProvider.GetRequiredService<ILogger<SampleProcessorApplication>>();

        // Simulate some processing time
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        // Process the data (simplified from original implementation)
        logger.LogInformation(
            "Sample processor processing data. ProcessorId: {ProcessorId}, StepId: {StepId}",
            processorId, stepId);

        // Initialize sample data - will be assigned from DeliveryEntity.Payload if available
        var sampleData = "This is sample data from the test processor";

        // Process DeliveryEntity if present and get its Payload
        var deliveryEntities = entities.OfType<DeliveryEntity>().ToList();
        foreach (var delivery in deliveryEntities)
        {
            logger.LogInformation(
                "Reading DeliveryEntity '{DeliveryName}' payload into sample data",
                delivery.Name);
            sampleData = delivery.Payload; // Assign DeliveryEntity.Payload TO sampleData
        }

        // Return processed data
        return new ProcessedActivityData
        {
            Result = "Sample processing completed successfully",
            Status = "completed",
            Data = new
            {
                processorId = processorId.ToString(),
                orchestratedFlowEntityId = orchestratedFlowEntityId.ToString(),
                entitiesProcessed = entities.Count,
                deliveryEntitiesProcessed = deliveryEntities.Count,
                processingDetails = new
                {
                    processedAt = DateTime.UtcNow,
                    processingDuration = "100ms",
                    inputDataReceived = true,
                    inputMetadataReceived = inputMetadata.HasValue,
                    sampleData = sampleData,
                    entityTypes = entities.Select(e => e.GetType().Name).Distinct().ToArray(),
                    deliveryEntities = deliveryEntities.Select(d => new
                    {
                        name = d.Name,
                        version = d.Version,
                        schemaId = d.SchemaId.ToString(),
                        payload = d.Payload,
                        payloadUsedAsSampleData = d.Payload == sampleData
                    }).ToArray()
                }
            },
            ProcessorName = "TestProcessor",
            Version = "1.0",
            ExecutionId = executionId == Guid.Empty ? Guid.NewGuid() : executionId
        };
    }
}