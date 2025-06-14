using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FlowOrchestrator.BaseProcessor.Application.Commands;
using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 TESTING: ExecutionId Cache Save with Generated GUID");
        Console.WriteLine("======================================================");
        Console.WriteLine("📋 This test verifies that when ProcessActivityDataAsync sets ExecutionId = Guid.NewGuid():");
        Console.WriteLine("   ✅ The new ExecutionId is extracted from the result JSON");
        Console.WriteLine("   ✅ Cache save uses the NEW ExecutionId (not the original empty one)");
        Console.WriteLine("   ✅ Success log shows the NEW ExecutionId");
        Console.WriteLine("   ✅ Cache key format: {OrchestratedFlowEntityId}_{StepId}_{NewExecutionId}");
        Console.WriteLine();
        Console.WriteLine("🔍 Expected behavior:");
        Console.WriteLine("   ❌ NO 'ExecutionId is empty - skipping cache save' message");
        Console.WriteLine("   ✅ Cache save should occur with the NEW ExecutionId");
        Console.WriteLine("   ✅ Success log should show the NEW ExecutionId (not 00000000-0000-0000-0000-000000000000)");
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
        
        // Generate specific test values for verification
        var processorId = Guid.Parse("b424a09d-196c-4b37-a2dd-b63fc0b7796c"); // BaseProcessor with schema-based configuration
        var orchestratedFlowEntityId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var executionId = Guid.Empty; // 🎯 Start with empty GUID, but processor will generate a new one!
        
        // Create test message with empty ExecutionId
        var testCommand = new ExecuteActivityCommand
        {
            ProcessorId = processorId,
            OrchestratedFlowEntityId = orchestratedFlowEntityId,
            StepId = stepId,
            ExecutionId = executionId,
            Entities = new List<BaseEntity>(),
            CorrelationId = "test-execution-id-cache-save-with-new-guid",
            CreatedAt = DateTime.UtcNow
        };
        
        Console.WriteLine("📤 Sending test message with ExecutionId = Guid.Empty...");
        Console.WriteLine($"   ProcessorId: {testCommand.ProcessorId}");
        Console.WriteLine($"   OrchestratedFlowEntityId: {testCommand.OrchestratedFlowEntityId}");
        Console.WriteLine($"   StepId: {testCommand.StepId}");
        Console.WriteLine($"   ExecutionId: {testCommand.ExecutionId} (Empty GUID - will be replaced by processor)");
        Console.WriteLine($"   CorrelationId: {testCommand.CorrelationId}");
        Console.WriteLine();
        Console.WriteLine("🔍 Expected cache behavior:");
        Console.WriteLine("   1. Processor generates NEW ExecutionId with Guid.NewGuid()");
        Console.WriteLine("   2. ExtractExecutionIdFromResult extracts the NEW ExecutionId from JSON");
        Console.WriteLine("   3. Cache save uses the NEW ExecutionId (not the original empty one)");
        Console.WriteLine("   4. Success log shows the NEW ExecutionId");
        Console.WriteLine();
        Console.WriteLine("🔍 Expected cache key format: {OrchestratedFlowEntityId}_{StepId}_{NewExecutionId}");
        Console.WriteLine($"   Expected key pattern: {orchestratedFlowEntityId}_{stepId}_[NEW-GUID-HERE]");
        Console.WriteLine($"   Expected map name: {processorId}");
        Console.WriteLine();
        
        await bus.Publish(testCommand);
        
        Console.WriteLine("✅ Test message sent successfully!");
        Console.WriteLine();
        Console.WriteLine("⏳ Waiting 8 seconds for processing...");
        Console.WriteLine("📊 Check the collector logs for verification:");
        Console.WriteLine("   ❌ Should NOT see: 'ExecutionId is empty - skipping cache save'");
        Console.WriteLine("   ✅ Should see: 'Successfully processed activity' with a NEW ExecutionId (not 00000000-0000-0000-0000-000000000000)");
        Console.WriteLine("   ✅ The NEW ExecutionId should be a valid GUID generated by the processor");
        Console.WriteLine();
        
        await Task.Delay(8000);
        
        await host.StopAsync();
        Console.WriteLine("🎉 Test completed! Check the collector output above for verification.");
        Console.WriteLine();
        Console.WriteLine("🔍 What to look for in the logs:");
        Console.WriteLine("   ✅ NO cache save skip message");
        Console.WriteLine("   ✅ Success message with ExecutionId that is NOT 00000000-0000-0000-0000-000000000000");
        Console.WriteLine("   ✅ The ExecutionId should be a valid GUID generated by ProcessActivityDataAsync");
    }
}
