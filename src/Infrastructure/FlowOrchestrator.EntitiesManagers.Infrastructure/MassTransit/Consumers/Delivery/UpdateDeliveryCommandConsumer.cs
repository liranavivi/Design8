using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Delivery;

public class UpdateDeliveryCommandConsumer : IConsumer<UpdateDeliveryCommand>
{
    private readonly IDeliveryEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateDeliveryCommandConsumer> _logger;

    public UpdateDeliveryCommandConsumer(
        IDeliveryEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateDeliveryCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateDeliveryCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing UpdateDeliveryCommand. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, RequestedBy: {RequestedBy}",
            command.Id, command.Version, command.Name, command.SchemaId, command.RequestedBy);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(command.Id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Delivery entity not found for update. Id: {Id}", command.Id);
                await context.RespondAsync(new UpdateDeliveryCommandResponse
                {
                    Success = false,
                    Message = $"Delivery entity with ID {command.Id} not found"
                });
                return;
            }

            var entity = new DeliveryEntity
            {
                Id = command.Id,
                Version = command.Version,
                Name = command.Name,
                Description = command.Description,
                SchemaId = command.SchemaId,
                Payload = command.Payload,
                UpdatedBy = command.RequestedBy,
                CreatedAt = existingEntity.CreatedAt,
                CreatedBy = existingEntity.CreatedBy
            };

            var updated = await _repository.UpdateAsync(entity);

            await _publishEndpoint.Publish(new DeliveryUpdatedEvent
            {
                Id = updated.Id,
                Version = updated.Version,
                Name = updated.Name,
                Description = updated.Description,
                SchemaId = updated.SchemaId,
                Payload = updated.Payload,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            stopwatch.Stop();
            _logger.LogInformation("Successfully processed UpdateDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                updated.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new UpdateDeliveryCommandResponse
            {
                Success = true,
                Message = "Delivery entity updated successfully"
            });
        }
        catch (ReferentialIntegrityException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Referential integrity violation during UpdateDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new UpdateDeliveryCommandResponse
            {
                Success = false,
                Message = $"Referential integrity violation: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing UpdateDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new UpdateDeliveryCommandResponse
            {
                Success = false,
                Message = $"Failed to update delivery entity: {ex.Message}"
            });
        }
    }
}

public class UpdateDeliveryCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
