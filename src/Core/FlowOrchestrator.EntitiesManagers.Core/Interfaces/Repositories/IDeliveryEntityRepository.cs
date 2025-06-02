using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IDeliveryEntityRepository : IBaseRepository<DeliveryEntity>
{
    Task<IEnumerable<DeliveryEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<DeliveryEntity>> GetByNameAsync(string name);
}
