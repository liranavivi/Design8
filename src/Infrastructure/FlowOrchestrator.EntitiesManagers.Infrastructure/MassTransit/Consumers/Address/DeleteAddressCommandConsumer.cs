using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Address;

public class DeleteAddressCommandConsumer : IConsumer<DeleteAddressCommand>
{
    private readonly IAddressEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteAddressCommandConsumer> _logger;

    public DeleteAddressCommandConsumer(
        IAddressEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteAddressCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteAddressCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing DeleteAddressCommand. Id: {Id}, RequestedBy: {RequestedBy}",
            command.Id, command.RequestedBy);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(command.Id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Address entity not found for deletion. Id: {Id}", command.Id);
                await context.RespondAsync(new DeleteAddressCommandResponse
                {
                    Success = false,
                    Message = $"Address entity with ID {command.Id} not found"
                });
                return;
            }

            var success = await _repository.DeleteAsync(command.Id);

            if (success)
            {
                await _publishEndpoint.Publish(new AddressDeletedEvent
                {
                    Id = command.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = command.RequestedBy
                });

                stopwatch.Stop();
                _logger.LogInformation("Successfully processed DeleteAddressCommand. Id: {Id}, Duration: {Duration}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new DeleteAddressCommandResponse
                {
                    Success = true,
                    Message = "Address entity deleted successfully"
                });
            }
            else
            {
                stopwatch.Stop();
                _logger.LogWarning("Failed to delete address entity. Id: {Id}, Duration: {Duration}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new DeleteAddressCommandResponse
                {
                    Success = false,
                    Message = "Failed to delete address entity"
                });
            }
        }
        catch (ReferentialIntegrityException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Referential integrity violation during DeleteAddressCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new DeleteAddressCommandResponse
            {
                Success = false,
                Message = $"Referential integrity violation: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing DeleteAddressCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new DeleteAddressCommandResponse
            {
                Success = false,
                Message = $"Failed to delete address entity: {ex.Message}"
            });
        }
    }
}

public class DeleteAddressCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
