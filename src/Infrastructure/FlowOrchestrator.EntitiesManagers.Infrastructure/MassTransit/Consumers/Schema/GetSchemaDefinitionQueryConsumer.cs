using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Schema;

public class GetSchemaDefinitionQueryConsumer : IConsumer<GetSchemaDefinitionQuery>
{
    private readonly ISchemaEntityRepository _repository;
    private readonly ILogger<GetSchemaDefinitionQueryConsumer> _logger;

    public GetSchemaDefinitionQueryConsumer(
        ISchemaEntityRepository repository,
        ILogger<GetSchemaDefinitionQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetSchemaDefinitionQuery> context)
    {
        var query = context.Message;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Processing GetSchemaDefinitionQuery. SchemaId: {SchemaId}, RequestedBy: {RequestedBy}",
            query.SchemaId, query.RequestedBy);

        try
        {
            var entity = await _repository.GetByIdAsync(query.SchemaId);

            stopwatch.Stop();

            if (entity != null)
            {
                _logger.LogInformation("Successfully processed GetSchemaDefinitionQuery. Found schema Id: {Id}, Definition length: {DefinitionLength}, Duration: {Duration}ms",
                    entity.Id, entity.Definition?.Length ?? 0, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new GetSchemaDefinitionQueryResponse
                {
                    Success = true,
                    Definition = entity.Definition,
                    Message = "Schema definition retrieved successfully"
                });
            }
            else
            {
                _logger.LogWarning("Schema entity not found. SchemaId: {SchemaId}, Duration: {Duration}ms",
                    query.SchemaId, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new GetSchemaDefinitionQueryResponse
                {
                    Success = false,
                    Definition = null,
                    Message = $"Schema entity with ID {query.SchemaId} not found"
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing GetSchemaDefinitionQuery. SchemaId: {SchemaId}, Duration: {Duration}ms",
                query.SchemaId, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new GetSchemaDefinitionQueryResponse
            {
                Success = false,
                Definition = null,
                Message = $"Error retrieving schema definition: {ex.Message}"
            });
        }
    }
}
