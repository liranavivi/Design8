using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Step;

public class GetStepQueryConsumer : IConsumer<GetStepQuery>
{
    private readonly IStepEntityRepository _repository;
    private readonly ILogger<GetStepQueryConsumer> _logger;

    public GetStepQueryConsumer(IStepEntityRepository repository, ILogger<GetStepQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetStepQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetStepQuery");

        try
        {
            if (context.Message.Id.HasValue)
            {
                activity?.SetTag("query.type", "ById");
                activity?.SetTag("query.id", context.Message.Id.Value.ToString());

                var entity = await _repository.GetByIdAsync(context.Message.Id.Value);
                if (entity != null)
                    await context.RespondAsync(entity);
                else
                    await context.RespondAsync(new { Error = "Step not found", Type = "NotFound" });
            }
            else if (context.Message.ProcessorId.HasValue && context.Message.ProcessorId != Guid.Empty)
            {
                activity?.SetTag("query.type", "ByProcessorId");
                activity?.SetTag("query.processorId", context.Message.ProcessorId.ToString());

                var entities = await _repository.GetByProcessorIdAsync(context.Message.ProcessorId.Value);
                if (entities.Any())
                    await context.RespondAsync(entities);
                else
                    await context.RespondAsync(new { Error = "Steps not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Either Id or ProcessorId must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetStepQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetStepQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
