# Testing Guide for FlowOrchestrator.BaseProcessor.Application

## Test Results Summary

✅ **Application Build**: Successfully builds with .NET 9
✅ **Application Startup**: Starts up correctly and initializes all components
✅ **Configuration Loading**: Properly loads configuration from appsettings.json
✅ **MassTransit Setup**: Correctly configures message consumers and endpoints
✅ **OpenTelemetry Logging**: Logs are routed to OpenTelemetry collector (http://localhost:4317)
✅ **Tracing & Metrics**: OpenTelemetry tracing and metrics configured for collector
✅ **Error Handling**: Gracefully handles missing infrastructure dependencies

## Current Configuration

### Processor Configuration
- **Name**: TestProcessor
- **Version**: 1.0
- **Protocol ID**: `123e4567-e89b-12d3-a456-426614174000`
- **Composite Key**: `1.0_TestProcessor`

### Infrastructure Settings
- **RabbitMQ**: localhost:5672 (guest/guest)
- **Hazelcast**: localhost:5701
- **OpenTelemetry Collector**: localhost:4317 (OTLP endpoint)
- **Schema Validation**: Enabled with detailed schemas

### OpenTelemetry Configuration
- **Logs**: Routed to collector via OTLP exporter
- **Traces**: Routed to collector via OTLP exporter
- **Metrics**: Routed to collector via OTLP exporter
- **Service Name**: BaseProcessorApplication
- **Service Version**: 1.0.0
- **Service Namespace**: FlowOrchestrator
- **Console Logging**: Disabled in development (logs go to collector only)

## Test Scenarios

### 1. Basic Startup Test ✅

```bash
dotnet run --project src/Framework/FlowOrchestrator.BaseProcessor.Application/FlowOrchestrator.BaseProcessor.Application.csproj
```

**Expected Behavior**:
- Application starts successfully
- Displays framework information
- Configures MassTransit endpoints
- Attempts processor initialization
- Handles timeout gracefully when EntitiesManager API is not available

**Observed Results**:
- ✅ Application starts without crashes
- ✅ MassTransit endpoints configured correctly:
  - `base-processor-execute-activity`
  - `base-processor-health`
  - `base-processor-statistics`
- ✅ Processor initialization begins
- ✅ Graceful timeout handling when API unavailable

### 2. Configuration Validation ✅

**Input Schema**:
```json
{
  "type": "object",
  "properties": {
    "data": {
      "type": "object",
      "properties": {
        "entities": {"type": "array", "items": {"type": "object"}},
        "parameters": {"type": "object"},
        "context": {"type": "object"}
      }
    },
    "metadata": {
      "type": "object",
      "properties": {
        "source": {"type": "string"},
        "timestamp": {"type": "string", "format": "date-time"},
        "version": {"type": "string"}
      }
    }
  },
  "required": ["data"]
}
```

**Output Schema**:
```json
{
  "type": "object",
  "properties": {
    "result": {"type": "string"},
    "timestamp": {"type": "string", "format": "date-time"},
    "stepId": {"type": "string"},
    "executionId": {"type": "string"},
    "correlationId": {"type": ["string", "null"]},
    "status": {"type": "string", "enum": ["completed", "failed", "processing"]},
    "data": {
      "type": "object",
      "properties": {
        "processorId": {"type": "string"},
        "orchestratedFlowEntityId": {"type": "string"},
        "entitiesProcessed": {"type": "integer"},
        "processingDetails": {"type": "object"}
      }
    },
    "metadata": {
      "type": "object",
      "properties": {
        "processor": {"type": "string"},
        "version": {"type": "string"},
        "environment": {"type": "string"},
        "machineName": {"type": "string"}
      }
    }
  },
  "required": ["result", "timestamp", "status", "data", "metadata"]
}
```

## Full Integration Testing

### Prerequisites

1. **RabbitMQ Server**
   ```bash
   # Using Docker
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. **Hazelcast Server**
   ```bash
   # Using Docker
   docker run -d --name hazelcast -p 5701:5701 hazelcast/hazelcast:latest
   ```

3. **EntitiesManager API**
   ```bash
   dotnet run --project src/Presentation/FlowOrchestrator.EntitiesManagers.Api/FlowOrchestrator.EntitiesManagers.Api.csproj
   ```

4. **MongoDB (for EntitiesManager)**
   ```bash
   # Using Docker
   docker run -d --name mongodb -p 27017:27017 mongo:latest
   ```

### Integration Test Steps

1. **Start Infrastructure**
   ```bash
   # Start all required services
   docker-compose up -d  # If you have docker-compose setup
   ```

2. **Create Test Protocol**
   ```bash
   curl -X POST http://localhost:5000/api/protocols \
     -H "Content-Type: application/json" \
     -d '{
       "name": "TestProtocol",
       "description": "Test protocol for processor testing"
     }'
   ```

3. **Start Processor Application**
   ```bash
   dotnet run --project src/Framework/FlowOrchestrator.BaseProcessor.Application/FlowOrchestrator.BaseProcessor.Application.csproj
   ```

4. **Send Test Activity Message**
   ```bash
   # This would require a message publisher tool or another application
   # to send ExecuteActivityCommand messages to the processor
   ```

### Expected Full Integration Behavior

1. **Processor Registration**:
   - Processor queries EntitiesManager for existing processor with key `1.0_TestProcessor`
   - If not found, creates new processor via `CreateProcessorCommand`
   - Retrieves and stores processor ID

2. **Message Consumption**:
   - Listens for `ExecuteActivityCommand` messages
   - Filters messages by processor ID
   - Processes activities using the implemented `ExecuteActivityAsync` method

3. **Cache Operations**:
   - Retrieves input data from Hazelcast using map `{ProcessorId}` and key `{OrchestratedFlowEntityId}_{StepId}_{ExecutionId}`
   - Validates input against InputSchema
   - Saves output to cache after validation against OutputSchema

4. **Response Publishing**:
   - Publishes `ProcessorActivityResponse` with processing results
   - Publishes success/failure events

## Development Testing

For development without full infrastructure:

1. **Mock Services**: Create mock implementations of `ICacheService` and message bus
2. **Unit Tests**: Test individual components in isolation
3. **Integration Tests**: Test with real infrastructure components

## Troubleshooting

### Common Issues

1. **Connection Timeouts**: Check if RabbitMQ and Hazelcast are running
2. **Schema Validation Errors**: Verify JSON schemas are valid
3. **Processor Not Found**: Ensure EntitiesManager API is running and accessible
4. **Message Not Consumed**: Check RabbitMQ queues and processor ID matching

### Logs to Monitor

- Processor initialization logs
- MassTransit connection logs
- Hazelcast connection logs
- Schema validation logs
- Activity processing logs

## Next Steps

1. **Create Unit Tests**: Test individual components
2. **Create Integration Tests**: Test with real infrastructure
3. **Performance Testing**: Test under load
4. **Error Scenario Testing**: Test various failure conditions
5. **End-to-End Testing**: Test complete workflow from message to response
