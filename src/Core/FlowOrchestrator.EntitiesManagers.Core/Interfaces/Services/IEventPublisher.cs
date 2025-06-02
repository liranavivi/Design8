namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData) where T : class;
}
