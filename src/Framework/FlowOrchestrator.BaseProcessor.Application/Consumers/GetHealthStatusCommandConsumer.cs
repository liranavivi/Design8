using FlowOrchestrator.BaseProcessor.Application.Commands;
using FlowOrchestrator.BaseProcessor.Application.Constants;
using FlowOrchestrator.BaseProcessor.Application.Extensions;
using FlowOrchestrator.BaseProcessor.Application.Models;
using FlowOrchestrator.BaseProcessor.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.BaseProcessor.Application.Consumers;

/// <summary>
/// Consumer for GetHealthStatusCommand messages
/// Following EntitiesManager.Api consumer patterns
/// </summary>
public class GetHealthStatusCommandConsumer : IConsumer<GetHealthStatusCommand>
{
    private readonly IProcessorService _processorService;
    private readonly ILogger<GetHealthStatusCommandConsumer> _logger;
    private static readonly ActivitySource ActivitySource = new(ActivitySources.Services);

    public GetHealthStatusCommandConsumer(
        IProcessorService processorService,
        ILogger<GetHealthStatusCommandConsumer> logger)
    {
        _processorService = processorService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetHealthStatusCommand> context)
    {
        using var activity = ActivitySource.StartActivity("GetHealthStatusCommand");
        var command = context.Message;

        // Set telemetry tags following EntitiesManager.Api patterns
        activity?.SetMessageTags(nameof(GetHealthStatusCommand), nameof(GetHealthStatusCommandConsumer))
                ?.SetTag("request.id", command.RequestId.ToString());

        _logger.LogDebug("Processing GetHealthStatusCommand for ProcessorId: {ProcessorId}, RequestId: {RequestId}", 
            command.ProcessorId, command.RequestId);

        try
        {
            // Check if this message is for this processor instance
            if (!await _processorService.IsMessageForThisProcessorAsync(command.ProcessorId))
            {
                _logger.LogDebug(
                    "Health status request not for this processor instance. ProcessorId: {ProcessorId}, RequestId: {RequestId}",
                    command.ProcessorId, command.RequestId);
                return;
            }

            var healthStatus = await _processorService.GetHealthStatusAsync();

            _logger.LogDebug(
                "Health status retrieved for ProcessorId: {ProcessorId}, RequestId: {RequestId}, Status: {Status}",
                command.ProcessorId, command.RequestId, healthStatus.Status);

            // Set health check telemetry
            activity?.SetHealthCheckTags("processor", healthStatus.Status, healthStatus.Uptime);

            // Respond with health status
            await context.RespondAsync(healthStatus);
        }
        catch (Exception ex)
        {
            activity?.SetErrorTags(ex);

            _logger.LogError(ex, "Failed to get health status for ProcessorId: {ProcessorId}, RequestId: {RequestId}", 
                command.ProcessorId, command.RequestId);

            // Respond with error status
            await context.RespondAsync(new ProcessorHealthStatusResponse
            {
                ProcessorId = command.ProcessorId,
                Status = HealthStatus.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Consumer for GetStatisticsCommand messages
/// Following EntitiesManager.Api consumer patterns
/// </summary>
public class GetStatisticsCommandConsumer : IConsumer<GetStatisticsCommand>
{
    private readonly IProcessorService _processorService;
    private readonly ILogger<GetStatisticsCommandConsumer> _logger;
    private static readonly ActivitySource ActivitySource = new(ActivitySources.Services);

    public GetStatisticsCommandConsumer(
        IProcessorService processorService,
        ILogger<GetStatisticsCommandConsumer> logger)
    {
        _processorService = processorService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetStatisticsCommand> context)
    {
        using var activity = ActivitySource.StartActivity("GetStatisticsCommand");
        var command = context.Message;

        // Set telemetry tags following EntitiesManager.Api patterns
        activity?.SetMessageTags(nameof(GetStatisticsCommand), nameof(GetStatisticsCommandConsumer))
                ?.SetTag("request.id", command.RequestId.ToString());

        _logger.LogDebug("Processing GetStatisticsCommand for ProcessorId: {ProcessorId}, RequestId: {RequestId}", 
            command.ProcessorId, command.RequestId);

        try
        {
            // Check if this message is for this processor instance
            if (!await _processorService.IsMessageForThisProcessorAsync(command.ProcessorId))
            {
                _logger.LogDebug(
                    "Statistics request not for this processor instance. ProcessorId: {ProcessorId}, RequestId: {RequestId}",
                    command.ProcessorId, command.RequestId);
                return;
            }

            var statistics = await _processorService.GetStatisticsAsync(command.FromDate, command.ToDate);

            _logger.LogDebug(
                "Statistics retrieved for ProcessorId: {ProcessorId}, RequestId: {RequestId}, TotalActivities: {TotalActivities}",
                command.ProcessorId, command.RequestId, statistics.TotalActivitiesProcessed);

            // Respond with statistics
            await context.RespondAsync(statistics);
        }
        catch (Exception ex)
        {
            activity?.SetErrorTags(ex);

            _logger.LogError(ex, "Failed to get statistics for ProcessorId: {ProcessorId}, RequestId: {RequestId}", 
                command.ProcessorId, command.RequestId);

            // Re-throw to trigger MassTransit error handling
            throw;
        }
    }
}
