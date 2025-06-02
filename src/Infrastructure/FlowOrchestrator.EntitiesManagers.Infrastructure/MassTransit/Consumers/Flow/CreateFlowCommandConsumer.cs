﻿using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Flow;

public class CreateFlowCommandConsumer : IConsumer<CreateFlowCommand>
{
    private readonly IFlowEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateFlowCommandConsumer> _logger;

    public CreateFlowCommandConsumer(
        IFlowEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateFlowCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateFlowCommand> context)
    {
        _logger.LogInformation("Processing CreateFlowCommand for {Version}_{Name}",
            context.Message.Version, context.Message.Name);

        try
        {
            var entity = new FlowEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                StepIds = context.Message.StepIds ?? new List<Guid>(),
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new FlowCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                StepIds = created.StepIds,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateFlowCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateFlowCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateFlowCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
            throw;
        }
    }
}
