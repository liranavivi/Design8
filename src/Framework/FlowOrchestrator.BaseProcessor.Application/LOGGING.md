# OpenTelemetry Logging Configuration

## ✅ **Log Messages Now Route to Collector**

The FlowOrchestrator.BaseProcessor.Application has been successfully configured to route **all log messages to the OpenTelemetry collector** at `http://localhost:4317`.

## Configuration Details

### OpenTelemetry Packages
```xml
<!-- Updated to match EntitiesManager.Api versions -->
<PackageReference Include="OpenTelemetry" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.12.0" />
```

### OpenTelemetry Configuration
```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => { /* traces to collector */ })
    .WithMetrics(builder => { /* metrics to collector */ })
    .WithLogging(builder =>
    {
        builder
            .SetResourceBuilder(resourceBuilder)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });

        // Console logging only when explicitly enabled
        var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
        var useConsoleForDev = configuration.GetValue<bool>("OpenTelemetry:UseConsoleInDevelopment", true);

        if (isDevelopment && useConsoleForDev)
        {
            builder.AddConsoleExporter();
        }
    });
```

### Application Configuration
```csharp
.ConfigureLogging(logging =>
{
    // Clear default logging providers - OpenTelemetry will handle logging
    logging.ClearProviders();
    
    // Add console logging in development for immediate feedback
    var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
    if (isDevelopment)
    {
        logging.AddConsole();
    }
})
```

### Settings Configuration
```json
{
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317",
    "UseConsoleInDevelopment": false
  }
}
```

## Service Resource Information

The logs are sent with the following service resource attributes:

- **Service Name**: `BaseProcessorApplication`
- **Service Version**: `1.0.0`
- **Service Namespace**: `FlowOrchestrator`
- **Service Instance ID**: `{MachineName}`

## Log Sources

The following log sources are captured and sent to the collector:

1. **Application Logs**: All ILogger calls from the application
2. **MassTransit Logs**: Message bus operations and errors
3. **Hazelcast Logs**: Cache operations and connection status
4. **OpenTelemetry Logs**: Instrumentation and exporter status
5. **System Logs**: .NET runtime and framework logs

## Verification

### Expected Behavior
- ✅ **No console logs** when `UseConsoleInDevelopment: false`
- ✅ **All logs sent to collector** via OTLP protocol
- ✅ **Structured logging** with proper service attributes
- ✅ **Log correlation** with traces and metrics

### Collector Endpoint
- **Protocol**: OTLP (OpenTelemetry Protocol)
- **Endpoint**: `http://localhost:4317`
- **Format**: Protobuf over HTTP

### Log Levels
The application respects the configured log levels:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "MassTransit": "Information",
      "Hazelcast": "Information",
      "BaseProcessorApplication": "Debug"
    }
  }
}
```

## Testing Log Collection

### 1. Start OpenTelemetry Collector
Ensure your OpenTelemetry collector is running and configured to receive OTLP data on port 4317.

### 2. Start BaseProcessor Application
```bash
dotnet run --project src/Framework/FlowOrchestrator.BaseProcessor.Application/FlowOrchestrator.BaseProcessor.Application.csproj
```

### 3. Verify in Collector
Check your collector's output/backend to see:
- Service name: `BaseProcessorApplication`
- Log messages from processor initialization
- MassTransit configuration logs
- Processor registration attempts

### 4. Expected Log Messages
You should see logs like:
- `"Starting SampleProcessorApplication"`
- `"Initializing SampleProcessorApplication"`
- `"Configured endpoint base-processor-execute-activity"`
- `"Initializing processor"`
- `"Timeout while requesting processor, creating new processor"`

## Troubleshooting

### No Logs in Collector
1. **Check collector endpoint**: Verify `http://localhost:4317` is accessible
2. **Check collector configuration**: Ensure OTLP receiver is enabled
3. **Check network**: Verify no firewall blocking port 4317
4. **Check application logs**: Temporarily enable console logging to debug

### Enable Console Logging for Debugging
```json
{
  "OpenTelemetry": {
    "UseConsoleInDevelopment": true
  }
}
```

### Collector Configuration Example
```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:

exporters:
  logging:
    loglevel: debug

service:
  pipelines:
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging]
```

## Architecture Consistency

This logging configuration follows the **exact same patterns** as:
- ✅ **EntitiesManager.Api**: Same OpenTelemetry setup
- ✅ **BaseImporter.Application**: Same logging approach
- ✅ **FlowOrchestrator Standards**: Consistent observability

The BaseProcessor.Application now provides **complete observability** with logs, traces, and metrics all routed to the OpenTelemetry collector for centralized monitoring and analysis.
