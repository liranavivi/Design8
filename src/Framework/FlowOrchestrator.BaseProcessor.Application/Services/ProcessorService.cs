using FlowOrchestrator.BaseProcessor.Application.Constants;
using FlowOrchestrator.BaseProcessor.Application.Extensions;
using FlowOrchestrator.BaseProcessor.Application.Models;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Schema;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace FlowOrchestrator.BaseProcessor.Application.Services;

/// <summary>
/// Core service for managing processor functionality and activity processing
/// </summary>
public class ProcessorService : IProcessorService
{
    private readonly IActivityExecutor _activityExecutor;
    private readonly ICacheService _cacheService;
    private readonly ISchemaValidator _schemaValidator;
    private readonly IBus _bus;
    private readonly ProcessorConfiguration _config;
    private readonly SchemaValidationConfiguration _validationConfig;
    private readonly ILogger<ProcessorService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _activitiesProcessedCounter;
    private readonly Counter<long> _activitiesSucceededCounter;
    private readonly Counter<long> _activitiesFailedCounter;
    private readonly Histogram<double> _activityDurationHistogram;
    private readonly DateTime _startTime;

    private Guid? _processorId;
    private readonly object _processorIdLock = new();

    public ProcessorService(
        IActivityExecutor activityExecutor,
        ICacheService cacheService,
        ISchemaValidator schemaValidator,
        IBus bus,
        IOptions<ProcessorConfiguration> config,
        IOptions<SchemaValidationConfiguration> validationConfig,
        ILogger<ProcessorService> logger)
    {
        _activityExecutor = activityExecutor;
        _cacheService = cacheService;
        _schemaValidator = schemaValidator;
        _bus = bus;
        _config = config.Value;
        _validationConfig = validationConfig.Value;
        _logger = logger;
        _activitySource = new ActivitySource(ActivitySources.Services);
        _meter = new Meter("BaseProcessorApplication.Services");
        _startTime = DateTime.UtcNow;

        // Initialize metrics
        _activitiesProcessedCounter = _meter.CreateCounter<long>(
            "processor_activities_processed_total",
            "Total number of activities processed");

        _activitiesSucceededCounter = _meter.CreateCounter<long>(
            "processor_activities_succeeded_total",
            "Total number of activities that succeeded");

        _activitiesFailedCounter = _meter.CreateCounter<long>(
            "processor_activities_failed_total",
            "Total number of activities that failed");

        _activityDurationHistogram = _meter.CreateHistogram<double>(
            "processor_activity_duration_seconds",
            "Duration of activity processing in seconds");
    }

    public async Task InitializeAsync()
    {
        using var activity = _activitySource.StartActivity("InitializeProcessor");

        _logger.LogInformation(
            "Initializing processor - {ProcessorName} v{ProcessorVersion}",
            _config.Name, _config.Version);

        try
        {
            // Try to get existing processor first
            var getQuery = new GetProcessorQuery
            {
                CompositeKey = _config.GetCompositeKey()
            };

            _logger.LogDebug("Requesting processor by composite key: {CompositeKey}", _config.GetCompositeKey());

            var response = await _bus.Request<GetProcessorQuery, FlowOrchestrator.EntitiesManagers.Core.Entities.ProcessorEntity>(
                getQuery, timeout: TimeSpan.FromSeconds(30));

            if (response.Message != null)
            {
                lock (_processorIdLock)
                {
                    _processorId = response.Message.Id;
                }

                _logger.LogInformation(
                    "Found existing processor. ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}",
                    _processorId, _config.GetCompositeKey());

                activity?.SetProcessorTags(_processorId.Value, _config.Name, _config.Version);

                // Retrieve schema definitions
                await RetrieveSchemaDefinitionsAsync();
            }
            else
            {
                _logger.LogInformation(
                    "Processor not found, creating new processor. CompositeKey: {CompositeKey}",
                    _config.GetCompositeKey());

                await CreateProcessorAsync();
            }
        }
        catch (RequestTimeoutException)
        {
            _logger.LogWarning(
                "Timeout while requesting processor, creating new processor. CompositeKey: {CompositeKey}",
                _config.GetCompositeKey());

            await CreateProcessorAsync();
        }
        catch (Exception ex)
        {
            activity?.SetErrorTags(ex);
            _logger.LogError(ex,
                "Failed to initialize processor. CompositeKey: {CompositeKey}",
                _config.GetCompositeKey());
            throw;
        }
    }

    private async Task CreateProcessorAsync()
    {
        var createCommand = new CreateProcessorCommand
        {
            Version = _config.Version,
            Name = _config.Name,
            Description = _config.Description,
            InputSchemaId = _config.InputSchemaId,
            OutputSchemaId = _config.OutputSchemaId,
            RequestedBy = "BaseProcessorApplication"
        };

        _logger.LogDebug("Publishing CreateProcessorCommand for {CompositeKey} with InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
            _config.GetCompositeKey(), createCommand.InputSchemaId, createCommand.OutputSchemaId);

        await _bus.Publish(createCommand);

        // Wait a bit and try to get the processor again
        await Task.Delay(TimeSpan.FromSeconds(2));

        var getQuery = new GetProcessorQuery
        {
            CompositeKey = _config.GetCompositeKey()
        };

        var response = await _bus.Request<GetProcessorQuery, FlowOrchestrator.EntitiesManagers.Core.Entities.ProcessorEntity>(
            getQuery, timeout: TimeSpan.FromSeconds(30));

        if (response.Message != null)
        {
            lock (_processorIdLock)
            {
                _processorId = response.Message.Id;
            }

            _logger.LogInformation(
                "Successfully created and retrieved processor. ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}",
                _processorId, _config.GetCompositeKey());

            // Retrieve schema definitions
            await RetrieveSchemaDefinitionsAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to create or retrieve processor with composite key: {_config.GetCompositeKey()}");
        }
    }

    private async Task RetrieveSchemaDefinitionsAsync()
    {
        _logger.LogInformation("Retrieving schema definitions for InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
            _config.InputSchemaId, _config.OutputSchemaId);

        try
        {
            // Retrieve input schema definition
            var inputSchemaQuery = new GetSchemaDefinitionQuery
            {
                SchemaId = _config.InputSchemaId,
                RequestedBy = "BaseProcessorApplication"
            };

            var inputSchemaResponse = await _bus.Request<GetSchemaDefinitionQuery, GetSchemaDefinitionQueryResponse>(
                inputSchemaQuery, timeout: TimeSpan.FromSeconds(30));

            if (inputSchemaResponse.Message.Success && !string.IsNullOrEmpty(inputSchemaResponse.Message.Definition))
            {
                _config.InputSchemaDefinition = inputSchemaResponse.Message.Definition;
                _logger.LogInformation("Successfully retrieved input schema definition. Length: {Length}",
                    _config.InputSchemaDefinition.Length);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve input schema definition. SchemaId: {SchemaId}, Message: {Message}",
                    _config.InputSchemaId, inputSchemaResponse.Message.Message);
            }

            // Retrieve output schema definition
            var outputSchemaQuery = new GetSchemaDefinitionQuery
            {
                SchemaId = _config.OutputSchemaId,
                RequestedBy = "BaseProcessorApplication"
            };

            var outputSchemaResponse = await _bus.Request<GetSchemaDefinitionQuery, GetSchemaDefinitionQueryResponse>(
                outputSchemaQuery, timeout: TimeSpan.FromSeconds(30));

            if (outputSchemaResponse.Message.Success && !string.IsNullOrEmpty(outputSchemaResponse.Message.Definition))
            {
                _config.OutputSchemaDefinition = outputSchemaResponse.Message.Definition;
                _logger.LogInformation("Successfully retrieved output schema definition. Length: {Length}",
                    _config.OutputSchemaDefinition.Length);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve output schema definition. SchemaId: {SchemaId}, Message: {Message}",
                    _config.OutputSchemaId, outputSchemaResponse.Message.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema definitions. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
                _config.InputSchemaId, _config.OutputSchemaId);
            // Don't throw - allow processor to continue without schema validation
        }
    }

    public async Task<Guid> GetProcessorIdAsync()
    {
        if (_processorId.HasValue)
        {
            return _processorId.Value;
        }

        // If not initialized, try to initialize
        await InitializeAsync();

        if (!_processorId.HasValue)
        {
            throw new InvalidOperationException("Processor ID is not available. Initialization may have failed.");
        }

        return _processorId.Value;
    }

    public async Task<bool> IsMessageForThisProcessorAsync(Guid processorId)
    {
        var myProcessorId = await GetProcessorIdAsync();
        return myProcessorId == processorId;
    }

    public string GetCacheMapName()
    {
        if (!_processorId.HasValue)
        {
            throw new InvalidOperationException("Processor ID is not available. Call InitializeAsync first.");
        }
        return _processorId.Value.ToString();
    }

    public string GetCacheKey(Guid orchestratedFlowEntityId, Guid stepId, Guid executionId)
    {
        return $"{orchestratedFlowEntityId}:{stepId}:{executionId}";
    }

    public async Task<string?> GetCachedDataAsync(Guid orchestratedFlowEntityId, Guid stepId, Guid executionId)
    {
        var mapName = GetCacheMapName();
        var key = GetCacheKey(orchestratedFlowEntityId, stepId, executionId);
        
        return await _cacheService.GetAsync(mapName, key);
    }

    public async Task SaveCachedDataAsync(Guid orchestratedFlowEntityId, Guid stepId, Guid executionId, string data)
    {
        var mapName = GetCacheMapName();
        var key = GetCacheKey(orchestratedFlowEntityId, stepId, executionId);

        await _cacheService.SetAsync(mapName, key, data);
    }

    private Guid ExtractExecutionIdFromResult(string resultData, Guid originalExecutionId)
    {
        try
        {
            // Parse the JSON result to extract the ExecutionId if it was updated by the processor
            var jsonDoc = JsonDocument.Parse(resultData);
            // The JSON uses camelCase naming policy, so the property is "executionId" (lowercase 'e')
            if (jsonDoc.RootElement.TryGetProperty("executionId", out var executionIdElement))
            {
                if (executionIdElement.TryGetGuid(out var extractedExecutionId))
                {
                    return extractedExecutionId;
                }
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, fall back to original ExecutionId
        }

        // Return the original ExecutionId if extraction fails or property doesn't exist
        return originalExecutionId;
    }

    public async Task<bool> ValidateInputDataAsync(string data)
    {
        if (!_validationConfig.EnableInputValidation)
        {
            return true;
        }

        if (string.IsNullOrEmpty(_config.InputSchemaDefinition))
        {
            _logger.LogWarning("Input schema definition is not available. Skipping validation.");
            return true;
        }

        return await _schemaValidator.ValidateAsync(data, _config.InputSchemaDefinition);
    }

    public async Task<bool> ValidateOutputDataAsync(string data)
    {
        if (!_validationConfig.EnableOutputValidation)
        {
            return true;
        }

        if (string.IsNullOrEmpty(_config.OutputSchemaDefinition))
        {
            _logger.LogWarning("Output schema definition is not available. Skipping validation.");
            return true;
        }

        return await _schemaValidator.ValidateAsync(data, _config.OutputSchemaDefinition);
    }

    public async Task<ProcessorActivityResponse> ProcessActivityAsync(ProcessorActivityMessage message)
    {
        using var activity = _activitySource.StartActivity("ProcessActivity");
        var stopwatch = Stopwatch.StartNew();

        activity?.SetActivityExecutionTags(
            message.OrchestratedFlowEntityId,
            message.StepId,
            message.ExecutionId,
            message.CorrelationId)
            ?.SetEntityTags(message.Entities.Count);

        _logger.LogInformation(
            "Processing activity. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}",
            message.ProcessorId, message.OrchestratedFlowEntityId, message.StepId, message.ExecutionId);

        try
        {
            string inputData;

            // Handle special case when ExecutionId is empty
            if (message.ExecutionId == Guid.Empty)
            {
                _logger.LogInformation(
                    "ExecutionId is empty - skipping cache retrieval and input validation. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}",
                    message.ProcessorId, message.OrchestratedFlowEntityId, message.StepId);

                // Skip cache retrieval and use empty string as input data
                inputData = string.Empty;
            }
            else
            {
                // 1. Retrieve data from cache (normal case)
                inputData = await GetCachedDataAsync(
                    message.OrchestratedFlowEntityId,
                    message.StepId,
                    message.ExecutionId) ?? string.Empty;

                if (string.IsNullOrEmpty(inputData))
                {
                    throw new InvalidOperationException(
                        $"No input data found in cache for key: {GetCacheKey(message.OrchestratedFlowEntityId, message.StepId, message.ExecutionId)}");
                }

                // 2. Validate input data against InputSchema (normal case)
                if (!await ValidateInputDataAsync(inputData))
                {
                    var errorMessage = "Input data validation failed against InputSchema";
                    _logger.LogError(errorMessage);

                    if (_validationConfig.FailOnValidationError)
                    {
                        throw new InvalidOperationException(errorMessage);
                    }
                }
            }

            // 3. Execute the activity
            var processorId = await GetProcessorIdAsync();
            var resultData = await _activityExecutor.ExecuteActivityAsync(
                processorId,
                message.OrchestratedFlowEntityId,
                message.StepId,
                message.ExecutionId,
                message.Entities,
                inputData,
                message.CorrelationId);

            // 4. Extract the ExecutionId from the result data (it may have been updated by the processor)
            var finalExecutionId = ExtractExecutionIdFromResult(resultData, message.ExecutionId);

            // 5. Validate output data against OutputSchema
            if (!await ValidateOutputDataAsync(resultData))
            {
                var errorMessage = "Output data validation failed against OutputSchema";
                _logger.LogError(errorMessage);

                if (_validationConfig.FailOnValidationError)
                {
                    throw new InvalidOperationException(errorMessage);
                }
            }

            // 6. Save result data to cache (skip if final ExecutionId is empty)
            if (finalExecutionId != Guid.Empty)
            {
                await SaveCachedDataAsync(
                    message.OrchestratedFlowEntityId,
                    message.StepId,
                    finalExecutionId,
                    resultData);
            }
            else
            {
                _logger.LogInformation(
                    "ExecutionId is empty - skipping cache save. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}",
                    message.ProcessorId, message.OrchestratedFlowEntityId, message.StepId);
            }

            stopwatch.Stop();

            // Update metrics
            _activitiesProcessedCounter.Add(1);
            _activitiesSucceededCounter.Add(1);
            _activityDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds);

            activity?.SetTag(ActivityTags.ActivityStatus, ActivityExecutionStatus.Completed.ToString())
                    ?.SetTag(ActivityTags.ActivityDuration, stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Successfully processed activity. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}, Duration: {Duration}ms",
                message.ProcessorId, message.OrchestratedFlowEntityId, message.StepId, finalExecutionId, stopwatch.ElapsedMilliseconds);

            return new ProcessorActivityResponse
            {
                ProcessorId = processorId,
                OrchestratedFlowEntityId = message.OrchestratedFlowEntityId,
                StepId = message.StepId,
                ExecutionId = finalExecutionId,
                Status = ActivityExecutionStatus.Completed,
                CorrelationId = message.CorrelationId,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Update metrics
            _activitiesProcessedCounter.Add(1);
            _activitiesFailedCounter.Add(1);
            _activityDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds);

            activity?.SetErrorTags(ex)
                    ?.SetTag(ActivityTags.ActivityStatus, ActivityExecutionStatus.Failed.ToString())
                    ?.SetTag(ActivityTags.ActivityDuration, stopwatch.ElapsedMilliseconds);

            _logger.LogError(ex,
                "Failed to process activity. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}, Duration: {Duration}ms",
                message.ProcessorId, message.OrchestratedFlowEntityId, message.StepId, message.ExecutionId, stopwatch.ElapsedMilliseconds);

            var processorId = _processorId ?? Guid.Empty;
            return new ProcessorActivityResponse
            {
                ProcessorId = processorId,
                OrchestratedFlowEntityId = message.OrchestratedFlowEntityId,
                StepId = message.StepId,
                ExecutionId = message.ExecutionId,
                Status = ActivityExecutionStatus.Failed,
                CorrelationId = message.CorrelationId,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<ProcessorHealthStatusResponse> GetHealthStatusAsync()
    {
        using var activity = _activitySource.StartActivity("GetHealthStatus");
        var processorId = await GetProcessorIdAsync();

        activity?.SetProcessorTags(processorId, _config.Name, _config.Version);

        try
        {
            var healthChecks = new Dictionary<string, HealthCheckResult>();

            // Check cache health
            var cacheHealthy = await _cacheService.IsHealthyAsync();
            healthChecks["cache"] = new HealthCheckResult
            {
                Status = cacheHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Description = "Hazelcast cache connectivity",
                Data = new Dictionary<string, object> { ["connected"] = cacheHealthy }
            };

            // Check message bus health (basic check)
            var busHealthy = _bus != null;
            healthChecks["messagebus"] = new HealthCheckResult
            {
                Status = busHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Description = "MassTransit message bus connectivity",
                Data = new Dictionary<string, object> { ["connected"] = busHealthy }
            };

            var overallStatus = healthChecks.Values.All(h => h.Status == HealthStatus.Healthy)
                ? HealthStatus.Healthy
                : HealthStatus.Unhealthy;

            return new ProcessorHealthStatusResponse
            {
                ProcessorId = processorId,
                Status = overallStatus,
                Message = overallStatus == HealthStatus.Healthy ? "All systems operational" : "Some systems are unhealthy",
                Details = healthChecks,
                Uptime = DateTime.UtcNow - _startTime,
                Version = _config.Version,
                Name = _config.Name
            };
        }
        catch (Exception ex)
        {
            activity?.SetErrorTags(ex);

            _logger.LogError(ex, "Failed to get health status for ProcessorId: {ProcessorId}", processorId);

            return new ProcessorHealthStatusResponse
            {
                ProcessorId = processorId,
                Status = HealthStatus.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                Uptime = DateTime.UtcNow - _startTime,
                Version = _config.Version,
                Name = _config.Name
            };
        }
    }

    public async Task<ProcessorStatisticsResponse> GetStatisticsAsync(DateTime? startTime, DateTime? endTime)
    {
        using var activity = _activitySource.StartActivity("GetStatistics");
        var processorId = await GetProcessorIdAsync();

        activity?.SetProcessorTags(processorId, _config.Name, _config.Version);

        try
        {
            // For now, return basic metrics
            // In a production system, you might want to store more detailed statistics
            var periodStart = startTime ?? _startTime;
            var periodEnd = endTime ?? DateTime.UtcNow;

            return new ProcessorStatisticsResponse
            {
                ProcessorId = processorId,
                TotalActivitiesProcessed = 0, // Would need to implement proper tracking
                SuccessfulActivities = 0,
                FailedActivities = 0,
                AverageExecutionTime = TimeSpan.Zero,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CollectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            activity?.SetErrorTags(ex);
            _logger.LogError(ex, "Failed to get statistics for ProcessorId: {ProcessorId}", processorId);
            throw;
        }
    }
}
