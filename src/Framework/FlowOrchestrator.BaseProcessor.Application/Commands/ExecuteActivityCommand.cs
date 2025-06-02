using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;

namespace FlowOrchestrator.BaseProcessor.Application.Commands;

/// <summary>
/// Command to execute an activity in the processor
/// Following EntitiesManager.Api command patterns
/// </summary>
public class ExecuteActivityCommand
{
    /// <summary>
    /// ID of the processor that should handle this activity
    /// </summary>
    public Guid ProcessorId { get; set; }

    /// <summary>
    /// ID of the orchestrated flow entity
    /// </summary>
    public Guid OrchestratedFlowEntityId { get; set; }

    /// <summary>
    /// ID of the step being executed
    /// </summary>
    public Guid StepId { get; set; }

    /// <summary>
    /// Unique execution ID for this activity instance
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Collection of base entities to process
    /// </summary>
    public List<BaseEntity> Entities { get; set; } = new();

    /// <summary>
    /// Optional correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional timeout for the activity execution
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Priority of the activity (higher numbers = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Additional metadata for the activity
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Command to get health status of a processor
/// Following EntitiesManager.Api command patterns
/// </summary>
public class GetHealthStatusCommand
{
    /// <summary>
    /// ID of the processor to check
    /// </summary>
    public Guid ProcessorId { get; set; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public Guid RequestId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the request was made
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Include detailed health check information
    /// </summary>
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Command to get statistics for a processor
/// Following EntitiesManager.Api command patterns
/// </summary>
public class GetStatisticsCommand
{
    /// <summary>
    /// ID of the processor to get statistics for
    /// </summary>
    public Guid ProcessorId { get; set; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public Guid RequestId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Start date for statistics period (null for all time)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for statistics period (null for current time)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Timestamp when the request was made
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Include detailed metrics breakdown
    /// </summary>
    public bool IncludeDetailedMetrics { get; set; } = false;
}
