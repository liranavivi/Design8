using FlowOrchestrator.BaseProcessor.Application.Commands;
using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using FlowOrchestrator.EntitiesManagers.Core.Entities;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestExecuteActivity;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 ExecuteActivityCommand Test with Schema-Based Processor");
        Console.WriteLine("=========================================================");
        Console.WriteLine();
        Console.WriteLine("📋 Test Details:");
        Console.WriteLine("   ProcessorId: b424a09d-196c-4b37-a2dd-b63fc0b7796c (BaseProcessor with MyInputSchema/MyOutputSchema)");
        Console.WriteLine("   InputSchemaId: 8b24db1b-cef9-4306-ac73-188136ff7040 (MyInputSchema)");
        Console.WriteLine("   OutputSchemaId: 61836404-fcb0-4c8b-bd0b-5a47c2eeea00 (MyOutputSchema)");
        Console.WriteLine();
        Console.WriteLine("🎯 Expected Behavior:");
        Console.WriteLine("   ✅ Processor should retrieve schema definitions automatically");
        Console.WriteLine("   ✅ Input data should be validated against MyInputSchema");
        Console.WriteLine("   ✅ Activity should be processed and cached");
        Console.WriteLine("   ✅ Collector logs should show cache storage operations");
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

        var publishEndpoint = host.Services.GetRequiredService<IPublishEndpoint>();

        // Create test entities that match the schema
        var testEntities = new List<BaseEntity>
        {
            new DeliveryEntity
            {
                Id = Guid.NewGuid(),
                Version = "1.0",
                Name = "TestDelivery",
                Description = "Test delivery for schema-based processor",
                SchemaId = Guid.Parse("61836404-fcb0-4c8b-bd0b-5a47c2eeea00"), // MyOutputSchema
                Payload = "{\"result\":\"test\",\"status\":\"processing\"}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "TestClient"
            }
        };

        // Create ExecuteActivityCommand with proper structure
        var command = new ExecuteActivityCommand
        {
            ProcessorId = Guid.Parse("b424a09d-196c-4b37-a2dd-b63fc0b7796c"), // BaseProcessor with our schemas
            OrchestratedFlowEntityId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            Entities = testEntities,
            CorrelationId = "test-schema-based-processor-001",
            CreatedAt = DateTime.UtcNow,
            Priority = 1,
            Metadata = new Dictionary<string, object>
            {
                ["testType"] = "schema-based-validation",
                ["inputSchema"] = "MyInputSchema",
                ["outputSchema"] = "MyOutputSchema",
                ["expectedValidation"] = true
            }
        };

        Console.WriteLine("📤 Publishing ExecuteActivityCommand...");
        Console.WriteLine($"   ProcessorId: {command.ProcessorId}");
        Console.WriteLine($"   ExecutionId: {command.ExecutionId}");
        Console.WriteLine($"   StepId: {command.StepId}");
        Console.WriteLine($"   CorrelationId: {command.CorrelationId}");
        Console.WriteLine($"   Entities Count: {command.Entities.Count}");
        Console.WriteLine();

        try
        {
            await publishEndpoint.Publish(command);
            Console.WriteLine("✅ ExecuteActivityCommand published successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to publish command: {ex.Message}");
            await host.StopAsync();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("⏳ Waiting 15 seconds for processing...");
        Console.WriteLine("📊 Monitor the OpenTelemetry Collector logs for:");
        Console.WriteLine("   🔍 Schema retrieval operations");
        Console.WriteLine("   🔍 Input validation against MyInputSchema");
        Console.WriteLine("   🔍 Activity processing logs");
        Console.WriteLine("   🔍 Cache storage operations (Hazelcast)");
        Console.WriteLine("   🔍 Success/failure status");
        Console.WriteLine();
        
        await Task.Delay(15000);
        
        await host.StopAsync();
        Console.WriteLine("🎉 Test completed!");
        Console.WriteLine();
        Console.WriteLine("🔍 What to verify in the collector logs:");
        Console.WriteLine("   ✅ 'Retrieving schema definitions for InputSchemaId: 8b24db1b-cef9-4306-ac73-188136ff7040'");
        Console.WriteLine("   ✅ 'Successfully retrieved input schema definition'");
        Console.WriteLine("   ✅ 'Successfully retrieved output schema definition'");
        Console.WriteLine("   ✅ 'Processing activity' with ExecutionId");
        Console.WriteLine("   ✅ Cache storage operations in Hazelcast");
        Console.WriteLine("   ✅ 'Successfully processed activity' with completion status");
    }
}
