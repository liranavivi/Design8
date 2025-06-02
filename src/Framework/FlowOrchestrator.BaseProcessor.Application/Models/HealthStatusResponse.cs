namespace FlowOrchestrator.BaseProcessor.Application.Models;

/// <summary>
/// Health status response for processor
/// </summary>
public class ProcessorHealthStatusResponse
{
    /// <summary>
    /// ID of the processor
    /// </summary>
    public Guid ProcessorId { get; set; }

    /// <summary>
    /// Overall health status
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Detailed health message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when health was checked
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Detailed health check results
    /// </summary>
    public Dictionary<string, HealthCheckResult> Details { get; set; } = new();

    /// <summary>
    /// Processor uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Version of the processor
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Name of the processor
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Statistics response for processor
/// </summary>
public class ProcessorStatisticsResponse
{
    /// <summary>
    /// ID of the processor
    /// </summary>
    public Guid ProcessorId { get; set; }

    /// <summary>
    /// Total number of activities processed
    /// </summary>
    public long TotalActivitiesProcessed { get; set; }

    /// <summary>
    /// Number of successful activities
    /// </summary>
    public long SuccessfulActivities { get; set; }

    /// <summary>
    /// Number of failed activities
    /// </summary>
    public long FailedActivities { get; set; }

    /// <summary>
    /// Average execution time for activities
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Start of the statistics period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of the statistics period
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// When these statistics were collected
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metrics
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Service is healthy and operational
    /// </summary>
    Healthy,

    /// <summary>
    /// Service is degraded but still operational
    /// </summary>
    Degraded,

    /// <summary>
    /// Service is unhealthy and may not be operational
    /// </summary>
    Unhealthy
}

/// <summary>
/// Individual health check result
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Status of this specific health check
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Description of the health check
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional data for this health check
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Duration of the health check
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Exception details if the health check failed
    /// </summary>
    public string? Exception { get; set; }
}
