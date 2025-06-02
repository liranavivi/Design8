using FlowOrchestrator.BaseProcessor.Application.Infrastructure;
using FlowOrchestrator.BaseProcessor.Application.Models;
using FlowOrchestrator.BaseProcessor.Application.Services;
using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlowOrchestrator.BaseProcessor.Application;

/// <summary>
/// Abstract base class for processor applications
/// </summary>
public abstract class BaseProcessorApplication : IActivityExecutor
{
    private IHost? _host;
    private ILogger<BaseProcessorApplication>? _logger;
    private ProcessorConfiguration? _config;

    /// <summary>
    /// Protected property to access the service provider for derived classes
    /// </summary>
    protected IServiceProvider ServiceProvider => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

    /// <summary>
    /// Main implementation of activity execution that handles common patterns
    /// </summary>
    public virtual async Task<string> ExecuteActivityAsync(
        Guid processorId,
        Guid orchestratedFlowEntityId,
        Guid stepId,
        Guid executionId,
        List<BaseEntity> entities,
        string inputData,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<BaseProcessorApplication>>();

        // Simulate some processing time
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        // Parse and validate input data
        JsonElement inputObject;
        JsonElement inputDataObj;
        JsonElement? inputMetadata = null;

        if (string.IsNullOrEmpty(inputData))
        {
            // Create default empty structures for empty input
            var emptyJson = "{\"data\":{},\"metadata\":{}}";
            inputObject = JsonSerializer.Deserialize<JsonElement>(emptyJson);
            inputDataObj = inputObject.GetProperty("data");
        }
        else
        {
            // Parse input data for normal case
            inputObject = JsonSerializer.Deserialize<JsonElement>(inputData);
            inputDataObj = inputObject.GetProperty("data");
            inputMetadata = inputObject.TryGetProperty("metadata", out var metadataElement) ? metadataElement : null;
        }

        logger.LogInformation(
            "Processing input data with {EntitiesInInput} entities from input data, {EntitiesInMessage} entities from message",
            inputDataObj.TryGetProperty("entities", out var entitiesElement) ? entitiesElement.GetArrayLength() : 0,
            entities.Count);

        // Call the abstract method that derived classes must implement
        var processedData = await ProcessActivityDataAsync(
            processorId,
            orchestratedFlowEntityId,
            stepId,
            executionId,
            entities,
            inputDataObj,
            inputMetadata,
            correlationId,
            cancellationToken);

        // Set the executionId of the processed data
        executionId = processedData.ExecutionId;

        // Create standard result structure
        var result = new
        {
            result = processedData.Result ?? "Processing completed successfully",
            timestamp = DateTime.UtcNow.ToString("O"),
            stepId = stepId.ToString(),
            executionId = executionId.ToString(),
            correlationId = correlationId,
            status = processedData.Status ?? "completed",
            data = processedData.Data ?? new { },
            metadata = new
            {
                processor = processedData.ProcessorName ?? GetType().Name,
                version = processedData.Version ?? "1.0",
                environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development",
                machineName = Environment.MachineName
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Abstract method that concrete processor implementations must override
    /// This is where the specific processor business logic should be implemented
    /// </summary>
    /// <param name="processorId">ID of the processor executing the activity</param>
    /// <param name="orchestratedFlowEntityId">ID of the orchestrated flow entity</param>
    /// <param name="stepId">ID of the step being executed</param>
    /// <param name="executionId">Unique execution ID for this activity instance</param>
    /// <param name="entities">Collection of base entities to process</param>
    /// <param name="inputData">Parsed input data object</param>
    /// <param name="inputMetadata">Optional metadata from input</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Processed data that will be incorporated into the standard result structure</returns>
    protected abstract Task<ProcessedActivityData> ProcessActivityDataAsync(
        Guid processorId,
        Guid orchestratedFlowEntityId,
        Guid stepId,
        Guid executionId,
        List<BaseEntity> entities,
        JsonElement inputData,
        JsonElement? inputMetadata,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Data structure for returning processed activity data
    /// </summary>
    protected class ProcessedActivityData
    {
        public string? Result { get; set; }
        public string? Status { get; set; }
        public object? Data { get; set; }
        public string? ProcessorName { get; set; }
        public string? Version { get; set; }
        public Guid ExecutionId { get; set; }
    }

    /// <summary>
    /// Main entry point for the processor application
    /// Sets up infrastructure and starts the application
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            // Build and configure the host (following EntitiesManager.Api pattern)
            _host = CreateHostBuilder(args).Build();

            // Get logger and configuration from DI container
            _logger = _host.Services.GetRequiredService<ILogger<BaseProcessorApplication>>();
            _config = _host.Services.GetRequiredService<IOptions<ProcessorConfiguration>>().Value;

            _logger.LogInformation("Starting {ApplicationName}", GetType().Name);

            _logger.LogInformation(
                "Initializing {ApplicationName} - {ProcessorName} v{ProcessorVersion}",
                GetType().Name, _config.Name, _config.Version);

            _logger.LogInformation("Starting host services (MassTransit, Hazelcast, etc.)...");

            // Start the host first (this will start MassTransit consumers)
            await _host.StartAsync();

            _logger.LogInformation("Host services started successfully. Now initializing processor...");

            // Initialize the processor service AFTER host is started
            var processorService = _host.Services.GetRequiredService<IProcessorService>();
            await processorService.InitializeAsync();

            _logger.LogInformation("Processor initialization completed successfully");

            _logger.LogInformation(
                "{ApplicationName} started successfully and is ready to process activities",
                GetType().Name);

            // Wait for shutdown signal
            var lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
            await WaitForShutdownAsync(lifetime.ApplicationStopping);

            _logger.LogInformation("Shutting down {ApplicationName}", GetType().Name);

            // Stop the host gracefully
            await _host.StopAsync(TimeSpan.FromSeconds(30));

            _logger.LogInformation("{ApplicationName} stopped successfully", GetType().Name);

            return 0;
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogCritical(ex, "Fatal error occurred in {ApplicationName}", GetType().Name);
            }
            else
            {
                Console.WriteLine($"Fatal error occurred in {GetType().Name}: {ex}");
            }

            return 1;
        }
        finally
        {
            _host?.Dispose();
        }
    }

    /// <summary>
    /// Creates and configures the host builder with all necessary services
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    protected virtual IHostBuilder CreateHostBuilder(string[] args)
    {
        // Find the project directory by looking for the .csproj file
        var currentDir = Directory.GetCurrentDirectory();
        var projectDir = FindProjectDirectory(currentDir);

        return Host.CreateDefaultBuilder(args)
            .UseContentRoot(projectDir)
            .UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
            .ConfigureLogging(logging =>
            {
                // Clear default logging providers - OpenTelemetry will handle logging
                logging.ClearProviders();
            })
            .ConfigureServices((context, services) =>
            {
                // Configure application settings
                services.Configure<ProcessorConfiguration>(
                    context.Configuration.GetSection("ProcessorConfiguration"));
                services.Configure<RabbitMQConfiguration>(
                    context.Configuration.GetSection("RabbitMQ"));
                services.Configure<Models.ProcessorHazelcastConfiguration>(
                    context.Configuration.GetSection("Hazelcast"));
                services.Configure<SchemaValidationConfiguration>(
                    context.Configuration.GetSection("SchemaValidation"));

                // Add core services
                services.AddSingleton<IActivityExecutor>(this);
                services.AddSingleton<IProcessorService, ProcessorService>();
                services.AddSingleton<ISchemaValidator, SchemaValidator>();

                // Add infrastructure services
                services.AddMassTransitWithRabbitMq(context.Configuration);
                services.AddHazelcastClient(context.Configuration);
                services.AddOpenTelemetryObservability(context.Configuration);

                // Allow derived classes to add custom services
                ConfigureServices(services, context.Configuration);
            });
    }

    /// <summary>
    /// Virtual method that derived classes can override to add custom services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    protected virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Default implementation does nothing
        // Derived classes can override to add custom services
    }

    /// <summary>
    /// Finds the project directory by looking for the .csproj file
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from</param>
    /// <returns>Project directory path</returns>
    private static string FindProjectDirectory(string startDirectory)
    {
        var currentDir = new DirectoryInfo(startDirectory);

        // Look for .csproj file in current directory and parent directories
        while (currentDir != null)
        {
            var csprojFiles = currentDir.GetFiles("*.csproj");
            if (csprojFiles.Length > 0)
            {
                return currentDir.FullName;
            }

            // Check if we're in the BaseProcessor.Application directory specifically
            if (currentDir.Name == "FlowOrchestrator.BaseProcessor.Application")
            {
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
        }

        // Fallback: try to find the BaseProcessor.Application directory
        var baseDir = startDirectory;
        var targetPath = Path.Combine(baseDir, "src", "Framework", "FlowOrchestrator.BaseProcessor.Application");
        if (Directory.Exists(targetPath))
        {
            return targetPath;
        }

        // Final fallback: use current directory
        return startDirectory;
    }

    /// <summary>
    /// Waits for shutdown signal
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the wait operation</returns>
    private static async Task WaitForShutdownAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        cancellationToken.Register(() => tcs.SetResult(true));

        // Also listen for Ctrl+C
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            tcs.SetResult(true);
        };

        await tcs.Task;
    }
}


