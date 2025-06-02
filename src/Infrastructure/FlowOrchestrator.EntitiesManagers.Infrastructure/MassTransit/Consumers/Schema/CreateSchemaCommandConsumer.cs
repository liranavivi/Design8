using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Schema;

public class CreateSchemaCommandConsumer : IConsumer<CreateSchemaCommand>
{
    private readonly ISchemaEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateSchemaCommandConsumer> _logger;

    public CreateSchemaCommandConsumer(
        ISchemaEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateSchemaCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateSchemaCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing CreateSchemaCommand. Version: {Version}, Name: {Name}, Definition: {Definition}, RequestedBy: {RequestedBy}",
            command.Version, command.Name, command.Definition, command.RequestedBy);

        try
        {
            var entity = new SchemaEntity
            {
                Version = command.Version,
                Name = command.Name,
                Description = command.Description,
                Definition = command.Definition,
                CreatedBy = command.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new SchemaCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                Definition = created.Definition,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            stopwatch.Stop();
            _logger.LogInformation("Successfully processed CreateSchemaCommand. Id: {Id}, Duration: {Duration}ms",
                created.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new CreateSchemaCommandResponse
            {
                Success = true,
                Id = created.Id,
                Message = "Schema entity created successfully"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing CreateSchemaCommand. Version: {Version}, Name: {Name}, Definition: {Definition}, Duration: {Duration}ms",
                command.Version, command.Name, command.Definition, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new CreateSchemaCommandResponse
            {
                Success = false,
                Id = Guid.Empty,
                Message = $"Failed to create schema entity: {ex.Message}"
            });
        }
    }
}

public class CreateSchemaCommandResponse
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
