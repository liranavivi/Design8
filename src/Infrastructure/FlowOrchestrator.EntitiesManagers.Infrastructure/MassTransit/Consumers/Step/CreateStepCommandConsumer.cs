using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Step;

public class CreateStepCommandConsumer : IConsumer<CreateStepCommand>
{
    private readonly IStepEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateStepCommandConsumer> _logger;

    public CreateStepCommandConsumer(
        IStepEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateStepCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateStepCommand> context)
    {
        _logger.LogInformation("Processing CreateStepCommand for ProcessorId: {ProcessorId}",
            context.Message.ProcessorId);

        try
        {
            var entity = new StepEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                ProcessorId = context.Message.ProcessorId,
                NextStepIds = context.Message.NextStepIds ?? new List<Guid>(),
                Description = context.Message.Description,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new StepCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                ProcessorId = created.ProcessorId,
                NextStepIds = created.NextStepIds,
                Description = created.Description,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateStepCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateStepCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateStepCommand for ProcessorId: {ProcessorId}",
                context.Message.ProcessorId);
            throw;
        }
    }
}
