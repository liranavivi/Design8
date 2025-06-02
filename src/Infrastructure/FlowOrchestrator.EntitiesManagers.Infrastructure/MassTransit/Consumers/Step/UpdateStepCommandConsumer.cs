﻿using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Step;

public class UpdateStepCommandConsumer : IConsumer<UpdateStepCommand>
{
    private readonly IStepEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateStepCommandConsumer> _logger;

    public UpdateStepCommandConsumer(
        IStepEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateStepCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateStepCommand> context)
    {
        _logger.LogInformation("Processing UpdateStepCommand for ID {Id}", context.Message.Id);

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                _logger.LogWarning("StepEntity with ID {Id} not found for update", context.Message.Id);
                await context.RespondAsync(new { Error = "Entity not found", Success = false });
                return;
            }

            // Update properties
            existing.Version = context.Message.Version;
            existing.Name = context.Message.Name;
            existing.ProcessorId = context.Message.ProcessorId;
            existing.NextStepIds = context.Message.NextStepIds ?? new List<Guid>();
            existing.Description = context.Message.Description;
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new StepUpdatedEvent
            {
                Id = updated.Id,
                Version = updated.Version,
                Name = updated.Name,
                ProcessorId = updated.ProcessorId,
                NextStepIds = updated.NextStepIds,
                Description = updated.Description,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed UpdateStepCommand for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in UpdateStepCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateStepCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
