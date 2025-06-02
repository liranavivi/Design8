using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Schema;

public class GetSchemaQueryConsumer : IConsumer<GetSchemaQuery>
{
    private readonly ISchemaEntityRepository _repository;
    private readonly ILogger<GetSchemaQueryConsumer> _logger;

    public GetSchemaQueryConsumer(
        ISchemaEntityRepository repository,
        ILogger<GetSchemaQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetSchemaQuery> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var query = context.Message;

        _logger.LogInformation("Processing GetSchemaQuery. Id: {Id}, CompositeKey: {CompositeKey}",
            query.Id, query.CompositeKey);

        try
        {
            SchemaEntity? entity = null;

            if (query.Id.HasValue)
            {
                entity = await _repository.GetByIdAsync(query.Id.Value);
            }
            else if (!string.IsNullOrEmpty(query.CompositeKey))
            {
                entity = await _repository.GetByCompositeKeyAsync(query.CompositeKey);
            }

            stopwatch.Stop();

            if (entity != null)
            {
                _logger.LogInformation("Successfully processed GetSchemaQuery. Found entity Id: {Id}, Duration: {Duration}ms",
                    entity.Id, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new GetSchemaQueryResponse
                {
                    Success = true,
                    Entity = entity,
                    Message = "Schema entity found"
                });
            }
            else
            {
                _logger.LogInformation("Schema entity not found. Id: {Id}, CompositeKey: {CompositeKey}, Duration: {Duration}ms",
                    query.Id, query.CompositeKey, stopwatch.ElapsedMilliseconds);

                await context.RespondAsync(new GetSchemaQueryResponse
                {
                    Success = false,
                    Entity = null,
                    Message = "Schema entity not found"
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing GetSchemaQuery. Id: {Id}, CompositeKey: {CompositeKey}, Duration: {Duration}ms",
                query.Id, query.CompositeKey, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new GetSchemaQueryResponse
            {
                Success = false,
                Entity = null,
                Message = $"Error retrieving schema entity: {ex.Message}"
            });
        }
    }
}

public class GetSchemaQueryResponse
{
    public bool Success { get; set; }
    public SchemaEntity? Entity { get; set; }
    public string Message { get; set; } = string.Empty;
}
