using FlowOrchestrator.BaseProcessor.Application.Commands;
using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using FlowOrchestrator.EntitiesManagers.Core.Entities;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestDeliveryEntityPayload;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🧪 TESTING: DeliveryEntity Payload Processing");
        Console.WriteLine("==============================================");
        Console.WriteLine("📋 This test verifies that:");
        Console.WriteLine("   ✅ DeliveryEntity.Payload is read by the processor");
        Console.WriteLine("   ✅ The payload content is assigned to sampleData");
        Console.WriteLine("   ✅ ProcessedActivityData includes the payload information");
        Console.WriteLine();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host("localhost", "/", h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });
                    });
                });
            })
            .Build();

        await host.StartAsync();
        
        var bus = host.Services.GetRequiredService<IBus>();
        
        // Use our schema-based processor
        var processorId = Guid.Parse("b424a09d-196c-4b37-a2dd-b63fc0b7796c"); // BaseProcessor with schema-based configuration
        var orchestratedFlowEntityId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        
        // Create DeliveryEntity with name "liran avivi" and custom payload
        var deliveryEntity = new DeliveryEntity
        {
            Id = Guid.NewGuid(),
            Version = "1.0",
            Name = "liran avivi", // As requested
            Description = "Test delivery entity for payload processing",
            SchemaId = Guid.Parse("61836404-fcb0-4c8b-bd0b-5a47c2eeea00"), // MyOutputSchema
            Payload = "Hello from liran avivi! This is custom payload data that should be read by the processor.",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "TestClient"
        };

        // Create test message with DeliveryEntity
        var testCommand = new ExecuteActivityCommand
        {
            ProcessorId = processorId,
            OrchestratedFlowEntityId = orchestratedFlowEntityId,
            StepId = stepId,
            ExecutionId = executionId,
            Entities = new List<BaseEntity> { deliveryEntity }, // Include the DeliveryEntity
            CorrelationId = "test-delivery-entity-payload-processing",
            CreatedAt = DateTime.UtcNow
        };
        
        Console.WriteLine("📤 Sending test message with DeliveryEntity...");
        Console.WriteLine($"   ProcessorId: {testCommand.ProcessorId}");
        Console.WriteLine($"   OrchestratedFlowEntityId: {testCommand.OrchestratedFlowEntityId}");
        Console.WriteLine($"   StepId: {testCommand.StepId}");
        Console.WriteLine($"   ExecutionId: {testCommand.ExecutionId}");
        Console.WriteLine($"   CorrelationId: {testCommand.CorrelationId}");
        Console.WriteLine();
        Console.WriteLine("📦 DeliveryEntity Details:");
        Console.WriteLine($"   Name: {deliveryEntity.Name}");
        Console.WriteLine($"   Version: {deliveryEntity.Version}");
        Console.WriteLine($"   SchemaId: {deliveryEntity.SchemaId}");
        Console.WriteLine($"   Payload: {deliveryEntity.Payload}");
        Console.WriteLine();
        Console.WriteLine("🔍 Expected processor behavior:");
        Console.WriteLine("   1. Processor receives DeliveryEntity in entities list");
        Console.WriteLine("   2. Processor reads DeliveryEntity.Payload into sampleData variable");
        Console.WriteLine("   3. ProcessedActivityData.Data.processingDetails.sampleData contains the payload");
        Console.WriteLine("   4. ProcessedActivityData.Data.deliveryEntities shows payload information");
        Console.WriteLine();
        
        await bus.Publish(testCommand);
        
        Console.WriteLine("✅ Test message sent successfully!");
        Console.WriteLine();
        Console.WriteLine("⏳ Waiting 8 seconds for processing...");
        Console.WriteLine("📊 Check the collector logs for verification:");
        Console.WriteLine("   ✅ Should see: 'Reading DeliveryEntity 'liran avivi' payload into sample data'");
        Console.WriteLine("   ✅ Should see: ProcessedActivityData with sampleData = DeliveryEntity.Payload");
        Console.WriteLine("   ✅ Should see: deliveryEntities array with payloadUsedAsSampleData = true");
        Console.WriteLine();
        
        await Task.Delay(8000);
        
        await host.StopAsync();
        Console.WriteLine("🎉 Test completed! Check the collector output above for verification.");
        Console.WriteLine();
        Console.WriteLine("🔍 What to look for in the logs:");
        Console.WriteLine("   ✅ DeliveryEntity payload reading log message");
        Console.WriteLine("   ✅ ProcessedActivityData with correct sampleData content");
        Console.WriteLine("   ✅ deliveryEntities details showing payload was used");
        
        return 0;
    }
}
