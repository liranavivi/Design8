using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Address;

public class CreateAddressCommandConsumer : IConsumer<CreateAddressCommand>
{
    private readonly IAddressEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateAddressCommandConsumer> _logger;

    public CreateAddressCommandConsumer(
        IAddressEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateAddressCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateAddressCommand> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var command = context.Message;

        _logger.LogInformation("Processing CreateAddressCommand. Address: {Address}, Version: {Version}, Name: {Name}, RequestedBy: {RequestedBy}",
            command.Address, command.Version, command.Name, command.RequestedBy);

        try
        {
            var entity = new AddressEntity
            {
                Address = command.Address,
                Version = command.Version,
                Name = command.Name,
                Description = command.Description,
                Configuration = command.Configuration ?? new Dictionary<string, object>(),
                SchemaId = command.SchemaId,
                CreatedBy = command.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new AddressCreatedEvent
            {
                Id = created.Id,
                Address = created.Address,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                Configuration = created.Configuration,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            stopwatch.Stop();
            _logger.LogInformation("Successfully processed CreateAddressCommand. Id: {Id}, Duration: {Duration}ms",
                created.Id, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new CreateAddressCommandResponse
            {
                Success = true,
                Id = created.Id,
                Message = "Address entity created successfully"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing CreateAddressCommand. Address: {Address}, Version: {Version}, Name: {Name}, Duration: {Duration}ms",
                command.Address, command.Version, command.Name, stopwatch.ElapsedMilliseconds);

            await context.RespondAsync(new CreateAddressCommandResponse
            {
                Success = false,
                Id = Guid.Empty,
                Message = $"Failed to create address entity: {ex.Message}"
            });
        }
    }
}

public class CreateAddressCommandResponse
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
