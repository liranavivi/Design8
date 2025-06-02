using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using MassTransit;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Services;

public class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        await _publishEndpoint.Publish(eventData);
    }
}
