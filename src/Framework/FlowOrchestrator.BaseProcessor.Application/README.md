# FlowOrchestrator.BaseProcessor.Application

A .NET Core 9 console application framework that provides an abstract base for creating processor services in the FlowOrchestrator ecosystem.

## Overview

The BaseProcessor.Application provides a robust foundation for building processor services that:

- **Process Activities**: Handle activity messages containing ProcessorId, OrchestratedFlowEntityId, StepId, ExecutionId, and collections of BaseEntity objects
- **Cache Integration**: Use Hazelcast for shared cache memory with map pattern `{ProcessorId}` and key pattern `{OrchestratedFlowEntityId}_{StepId}_{ExecutionId}`
- **Schema Validation**: Validate input data against InputSchema before processing and output data against OutputSchema after processing
- **Message Bus Integration**: Use MassTransit with RabbitMQ for message consumption and publishing
- **Observability**: Integrate with OpenTelemetry for comprehensive logging, metrics, and tracing
- **Health Monitoring**: Provide health check and statistics endpoints

## Architecture

### Key Components

1. **BaseProcessorApplication**: Abstract base class that concrete processors inherit from
2. **IActivityExecutor**: Interface that concrete processors must implement
3. **ProcessorService**: Core service handling processor lifecycle and activity processing
4. **Message Consumers**: Handle ExecuteActivityCommand, health checks, and statistics requests
5. **Cache Service**: Hazelcast integration for data storage and retrieval
6. **Schema Validator**: JSON schema validation for input and output data

### Startup Behavior

1. Retrieve processorId from ProcessorEntityManager using composite key format: `{Version}_{Name}`
2. If processor doesn't exist, publish CreateProcessorCommand to create it
3. Save ProcessorEntity ID for future message filtering

### Activity Processing Workflow

1. **Message Consumption**: Consume activity messages addressed by processor ID
2. **Data Retrieval**: Get cached data using map `{ProcessorId}` and key `{OrchestratedFlowEntityId}_{StepId}_{ExecutionId}`
3. **Input Validation**: Validate retrieved data against InputSchema
4. **Activity Execution**: Call the overridden activity method
5. **Output Validation**: Validate result data against OutputSchema
6. **Data Storage**: Save result to cache using same map and key pattern
7. **Response Publishing**: Publish response message with ProcessorId, OrchestratedFlowEntityId, StepId, ExecutionId

## Getting Started

### 1. Create a Concrete Processor

```csharp
public class MyCustomProcessor : BaseProcessorApplication
{
    public static async Task<int> Main(string[] args)
    {
        var app = new MyCustomProcessor();
        return await app.RunAsync(args);
    }

    public override async Task<string> ExecuteActivityAsync(
        Guid processorId,
        Guid orchestratedFlowEntityId,
        Guid stepId,
        Guid executionId,
        List<BaseEntity> entities,
        string inputData,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Your custom processing logic here
        var result = new
        {
            result = "Processing completed",
            timestamp = DateTime.UtcNow.ToString("O"),
            stepId = stepId.ToString(),
            executionId = executionId.ToString(),
            status = "completed",
            data = new { /* your result data */ }
        };

        return JsonSerializer.Serialize(result);
    }
}
```

### 2. Configure Your Processor

Update `appsettings.json`:

```json
{
  "ProcessorConfiguration": {
    "Version": "1.0",
    "Name": "MyCustomProcessor",
    "Description": "My custom processor for specific business logic",
    "InputSchemaId": "your-input-schema-guid",
    "OutputSchemaId": "your-output-schema-guid"
  }
}
```

### 3. Add Custom Services (Optional)

```csharp
protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
    
    // Add your custom services
    services.AddSingleton<IMyCustomService, MyCustomService>();
    services.AddHttpClient<IExternalApiClient, ExternalApiClient>();
}
```

## Configuration

### Processor Configuration

- **Version**: Version of the processor (used in composite key)
- **Name**: Name of the processor (used in composite key)
- **Description**: Description of processor functionality
- **InputSchemaId**: Schema ID for validating input data (retrieved from SchemasManager)
- **OutputSchemaId**: Schema ID for validating output data (retrieved from SchemasManager)

### Infrastructure Configuration

- **RabbitMQ**: Message bus configuration
- **Hazelcast**: Distributed cache configuration
- **OpenTelemetry**: Observability configuration
- **SchemaValidation**: Validation behavior configuration

## Message Handling

### Activity Messages

The processor consumes `ExecuteActivityCommand` messages containing:
- ProcessorId
- OrchestratedFlowEntityId
- StepId
- ExecutionId
- Collection of BaseEntity objects
- Optional correlation ID

### Response Messages

After processing, publishes `ProcessorActivityResponse` with:
- ProcessorId
- OrchestratedFlowEntityId
- StepId
- ExecutionId
- Status (Completed/Failed)
- Optional error message
- Processing duration

### Health Check Messages

Responds to health check requests with:
- Processor status
- Infrastructure health (cache, message bus)
- Uptime information
- Detailed health check results

## Error Handling

- **Validation Errors**: Configurable behavior for schema validation failures
- **Processing Errors**: Automatic error response publishing
- **Infrastructure Errors**: Comprehensive error logging and telemetry
- **Retry Logic**: Built-in retry mechanisms for message processing

## Observability

### Logging

- Structured logging with correlation IDs
- Configurable log levels per component
- Integration with OpenTelemetry logging

### Metrics

- Activity processing counters
- Success/failure rates
- Processing duration histograms
- Infrastructure health metrics

### Tracing

- Distributed tracing across message flows
- Activity execution spans
- Cache operation tracing
- Schema validation tracing

## Examples

See the `Examples` folder for:
- `SampleProcessorApplication`: Basic processor implementation
- Configuration examples
- Custom service integration patterns

## Dependencies

- .NET 9.0
- MassTransit 8.1.1 (RabbitMQ transport)
- Hazelcast.Net 5.3.0
- OpenTelemetry 1.6.0
- Newtonsoft.Json.Schema 3.0.15
- FlowOrchestrator.EntitiesManagers.Core
- FlowOrchestrator.EntitiesManagers.Infrastructure

## Best Practices

1. **Schema Design**: Design comprehensive input/output schemas for validation
2. **Error Handling**: Implement proper error handling in your activity logic
3. **Logging**: Use structured logging with meaningful context
4. **Performance**: Consider async/await patterns for I/O operations
5. **Testing**: Write unit tests for your activity logic
6. **Configuration**: Use environment-specific configuration files
7. **Monitoring**: Monitor processor health and performance metrics
