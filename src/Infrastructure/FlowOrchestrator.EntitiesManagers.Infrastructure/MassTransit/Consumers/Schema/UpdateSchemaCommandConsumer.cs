using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Schema;

public class UpdateSchemaCommandConsumer : IConsumer<UpdateSchemaCommand>
{
    private readonly ISchemaEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateSchemaCommandConsumer> _logger;

    public UpdateSchemaCommandConsumer(
        ISchemaEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateSchemaCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateSchemaCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing UpdateSchemaCommand. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, RequestedBy: {RequestedBy}",
            command.Id, command.Version, command.Name, command.Definition, command.RequestedBy);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(command.Id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Schema entity not found for update. Id: {Id}", command.Id);
                await context.RespondAsync(new UpdateSchemaCommandResponse
                {
                    Success = false,
                    Message = $"Schema entity with ID {command.Id} not found"
                });
                return;
            }

            var entity = new SchemaEntity
            {
                Id = command.Id,
                Version = command.Version,
                Name = command.Name,
                Description = command.Description,
                Definition = command.Definition,
                UpdatedBy = command.RequestedBy,
                CreatedAt = existingEntity.CreatedAt,
                CreatedBy = existingEntity.CreatedBy
            };

            var updated = await _repository.UpdateAsync(entity);

            await _publishEndpoint.Publish(new SchemaUpdatedEvent
            {
                Id = updated.Id,
                Version = updated.Version,
                Name = updated.Name,
                Description = updated.Description,
                Definition = updated.Definition,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            stopwatch.Stop();
            _logger.LogInformation("Successfully processed UpdateSchemaCommand. Id: {Id}, Duration: {Duration}ms",
                updated.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new UpdateSchemaCommandResponse
            {
                Success = true,
                Message = "Schema entity updated successfully"
            });
        }
        catch (ReferentialIntegrityException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Referential integrity violation during UpdateSchemaCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new UpdateSchemaCommandResponse
            {
                Success = false,
                Message = $"Referential integrity violation: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing UpdateSchemaCommand. Id: {Id}, Duration: {Duration}ms",
                command.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new UpdateSchemaCommandResponse
            {
                Success = false,
                Message = $"Failed to update schema entity: {ex.Message}"
            });
        }
    }
}

public class UpdateSchemaCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
