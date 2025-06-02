using FlowOrchestrator.BaseProcessor.Application.Commands;
using FlowOrchestrator.BaseProcessor.Application.Constants;
using FlowOrchestrator.BaseProcessor.Application.Events;
using FlowOrchestrator.BaseProcessor.Application.Extensions;
using FlowOrchestrator.BaseProcessor.Application.Models;
using FlowOrchestrator.BaseProcessor.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.BaseProcessor.Application.Consumers;

/// <summary>
/// Consumer for ExecuteActivityCommand messages
/// Following EntitiesManager.Api consumer patterns
/// </summary>
public class ExecuteActivityCommandConsumer : IConsumer<ExecuteActivityCommand>
{
    private readonly IProcessorService _processorService;
    private readonly ILogger<ExecuteActivityCommandConsumer> _logger;
    private static readonly ActivitySource ActivitySource = new(ActivitySources.Services);

    public ExecuteActivityCommandConsumer(
        IProcessorService processorService,
        ILogger<ExecuteActivityCommandConsumer> logger)
    {
        _processorService = processorService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExecuteActivityCommand> context)
    {
        using var activity = ActivitySource.StartActivity("ExecuteActivityCommand");
        var command = context.Message;
        var stopwatch = Stopwatch.StartNew();

        // Set telemetry tags following EntitiesManager.Api patterns
        activity?.SetMessageTags(nameof(ExecuteActivityCommand), nameof(ExecuteActivityCommandConsumer), command.CorrelationId)
                ?.SetActivityExecutionTags(
                    command.OrchestratedFlowEntityId,
                    command.StepId,
                    command.ExecutionId,
                    command.CorrelationId)
                ?.SetEntityTags(command.Entities.Count);

        _logger.LogInformation(
            "Processing ExecuteActivityCommand. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}",
            command.ProcessorId, command.OrchestratedFlowEntityId, command.StepId, command.ExecutionId);

        try
        {
            // Check if this message is for this processor instance
            if (!await _processorService.IsMessageForThisProcessorAsync(command.ProcessorId))
            {
                _logger.LogDebug(
                    "Message not for this processor instance. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}",
                    command.ProcessorId, command.OrchestratedFlowEntityId, command.StepId, command.ExecutionId);
                return;
            }

            // Convert command to activity message
            var activityMessage = new ProcessorActivityMessage
            {
                ProcessorId = command.ProcessorId,
                OrchestratedFlowEntityId = command.OrchestratedFlowEntityId,
                StepId = command.StepId,
                ExecutionId = command.ExecutionId,
                Entities = command.Entities,
                CorrelationId = command.CorrelationId,
                CreatedAt = command.CreatedAt
            };

            // Process the activity
            var response = await _processorService.ProcessActivityAsync(activityMessage);

            stopwatch.Stop();

            // Set success telemetry
            activity?.SetTag(ActivityTags.ActivityStatus, response.Status.ToString())
                    ?.SetTag(ActivityTags.ActivityDuration, stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Successfully executed activity. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}, Status: {Status}, Duration: {Duration}ms",
                command.ProcessorId, command.OrchestratedFlowEntityId, command.StepId, command.ExecutionId, response.Status, stopwatch.ElapsedMilliseconds);

            // Publish response message
            await context.Publish(response);

            // Publish success event
            if (response.Status == ActivityExecutionStatus.Completed)
            {
                await context.Publish(new ActivityExecutedEvent
                {
                    ProcessorId = response.ProcessorId,
                    OrchestratedFlowEntityId = response.OrchestratedFlowEntityId,
                    StepId = response.StepId,
                    ExecutionId = response.ExecutionId,
                    CorrelationId = response.CorrelationId,
                    Duration = response.Duration,
                    Status = response.Status,
                    EntitiesProcessed = command.Entities.Count
                });
            }
            else
            {
                // Publish failure event
                await context.Publish(new ActivityFailedEvent
                {
                    ProcessorId = response.ProcessorId,
                    OrchestratedFlowEntityId = response.OrchestratedFlowEntityId,
                    StepId = response.StepId,
                    ExecutionId = response.ExecutionId,
                    CorrelationId = response.CorrelationId,
                    Duration = response.Duration,
                    ErrorMessage = response.ErrorMessage ?? "Unknown error",
                    EntitiesBeingProcessed = command.Entities.Count
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            activity?.SetErrorTags(ex)
                    ?.SetTag(ActivityTags.ActivityStatus, ActivityExecutionStatus.Failed.ToString())
                    ?.SetTag(ActivityTags.ActivityDuration, stopwatch.ElapsedMilliseconds);

            _logger.LogError(ex,
                "Failed to execute activity. ProcessorId: {ProcessorId}, OrchestratedFlowEntityId: {OrchestratedFlowEntityId}, StepId: {StepId}, ExecutionId: {ExecutionId}, Duration: {Duration}ms",
                command.ProcessorId, command.OrchestratedFlowEntityId, command.StepId, command.ExecutionId, stopwatch.ElapsedMilliseconds);

            // Publish error response
            var errorResponse = new ProcessorActivityResponse
            {
                ProcessorId = command.ProcessorId,
                OrchestratedFlowEntityId = command.OrchestratedFlowEntityId,
                StepId = command.StepId,
                ExecutionId = command.ExecutionId,
                Status = ActivityExecutionStatus.Failed,
                CorrelationId = command.CorrelationId,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };

            await context.Publish(errorResponse);

            // Publish failure event
            await context.Publish(new ActivityFailedEvent
            {
                ProcessorId = command.ProcessorId,
                OrchestratedFlowEntityId = command.OrchestratedFlowEntityId,
                StepId = command.StepId,
                ExecutionId = command.ExecutionId,
                CorrelationId = command.CorrelationId,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace,
                EntitiesBeingProcessed = command.Entities.Count
            });

            // Re-throw to trigger MassTransit error handling
            throw;
        }
    }
}
