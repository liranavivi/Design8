using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;

namespace FlowOrchestrator.BaseProcessor.Application.Services;

/// <summary>
/// Interface for the abstract activity execution logic that concrete processors must implement
/// </summary>
public interface IActivityExecutor
{
    /// <summary>
    /// Executes an activity with the provided parameters
    /// This method must be implemented by concrete processor applications
    /// </summary>
    /// <param name="processorId">ID of the processor executing the activity</param>
    /// <param name="orchestratedFlowEntityId">ID of the orchestrated flow entity</param>
    /// <param name="stepId">ID of the step being executed</param>
    /// <param name="executionId">Unique execution ID for this activity instance</param>
    /// <param name="entities">Collection of base entities to process</param>
    /// <param name="inputData">Input data retrieved from cache (validated against InputSchema)</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Result data that will be validated against OutputSchema and saved to cache</returns>
    Task<string> ExecuteActivityAsync(
        Guid processorId,
        Guid orchestratedFlowEntityId,
        Guid stepId,
        Guid executionId,
        List<BaseEntity> entities,
        string inputData,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
