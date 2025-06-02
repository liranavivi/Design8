using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Delivery;

public class DeleteDeliveryCommandConsumer : IConsumer<DeleteDeliveryCommand>
{
    private readonly IDeliveryEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteDeliveryCommandConsumer> _logger;

    public DeleteDeliveryCommandConsumer(
        IDeliveryEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteDeliveryCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteDeliveryCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing DeleteDeliveryCommand. Id: {Id}, RequestedBy: {RequestedBy}",
            command.Id, command.RequestedBy);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(command.Id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Delivery entity not found for deletion. Id: {Id}", command.Id);
                await context.RespondAsync(new DeleteDeliveryCommandResponse
                {
                    Success = false,
                    Message = $"Delivery entity with ID {command.Id} not found"
                });
                return;
            }

            var success = await _repository.DeleteAsync(command.Id);

            if (success)
            {
                await _publishEndpoint.Publish(new DeliveryDeletedEvent
                {
                    Id = command.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = command.RequestedBy
                });

                stopwatch.Stop();
                _logger.LogInformation("Successfully processed DeleteDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new DeleteDeliveryCommandResponse
                {
                    Success = true,
                    Message = "Delivery entity deleted successfully"
                });
            }
            else
            {
                stopwatch.Stop();
                _logger.LogWarning("Failed to delete delivery entity. Id: {Id}, Duration: {Duration}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new DeleteDeliveryCommandResponse
                {
                    Success = false,
                    Message = "Failed to delete delivery entity"
                });
            }
        }
        catch (ReferentialIntegrityException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Referential integrity violation during DeleteDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new DeleteDeliveryCommandResponse
            {
                Success = false,
                Message = $"Referential integrity violation: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing DeleteDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new DeleteDeliveryCommandResponse
            {
                Success = false,
                Message = $"Failed to delete delivery entity: {ex.Message}"
            });
        }
    }
}

public class DeleteDeliveryCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
