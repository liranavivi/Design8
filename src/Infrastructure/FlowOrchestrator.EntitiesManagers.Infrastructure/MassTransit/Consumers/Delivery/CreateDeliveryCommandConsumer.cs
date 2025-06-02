using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Delivery;

public class CreateDeliveryCommandConsumer : IConsumer<CreateDeliveryCommand>
{
    private readonly IDeliveryEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateDeliveryCommandConsumer> _logger;

    public CreateDeliveryCommandConsumer(
        IDeliveryEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateDeliveryCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateDeliveryCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing CreateDeliveryCommand. Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, RequestedBy: {RequestedBy}",
            command.Version, command.Name, command.SchemaId, command.RequestedBy);

        try
        {
            var entity = new DeliveryEntity
            {
                Version = command.Version,
                Name = command.Name,
                Description = command.Description,
                SchemaId = command.SchemaId,
                Payload = command.Payload,
                CreatedBy = command.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new DeliveryCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                SchemaId = created.SchemaId,
                Payload = created.Payload,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            stopwatch.Stop();
            _logger.LogInformation("Successfully processed CreateDeliveryCommand. Id: {Id}, Duration: {Duration}ms",
                created.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new CreateDeliveryCommandResponse
            {
                Success = true,
                Id = created.Id,
                Message = "Delivery entity created successfully"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing CreateDeliveryCommand. Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, Duration: {Duration}ms",
                command.Version, command.Name, command.SchemaId, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new CreateDeliveryCommandResponse
            {
                Success = false,
                Id = Guid.Empty,
                Message = $"Failed to create delivery entity: {ex.Message}"
            });
        }
    }
}

public class CreateDeliveryCommandResponse
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
