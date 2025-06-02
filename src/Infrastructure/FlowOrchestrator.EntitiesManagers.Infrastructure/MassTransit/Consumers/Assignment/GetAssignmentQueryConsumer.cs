﻿using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Assignment;

public class GetAssignmentQueryConsumer : IConsumer<GetAssignmentQuery>
{
    private readonly IAssignmentEntityRepository _repository;
    private readonly ILogger<GetAssignmentQueryConsumer> _logger;

    public GetAssignmentQueryConsumer(IAssignmentEntityRepository repository, ILogger<GetAssignmentQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetAssignmentQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetAssignmentQuery");

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
                    await context.RespondAsync(new { Error = "Assignment not found", Type = "NotFound" });
            }
            else if (!string.IsNullOrEmpty(context.Message.CompositeKey))
            {
                activity?.SetTag("query.type", "ByCompositeKey");
                activity?.SetTag("query.compositeKey", context.Message.CompositeKey);

                var entity = await _repository.GetByCompositeKeyAsync(context.Message.CompositeKey);
                if (entity != null)
                    await context.RespondAsync(entity);
                else
                    await context.RespondAsync(new { Error = "Assignment not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Either Id or CompositeKey must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetAssignmentQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetAssignmentQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
