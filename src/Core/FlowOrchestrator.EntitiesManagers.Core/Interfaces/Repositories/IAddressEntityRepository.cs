using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IAddressEntityRepository : IBaseRepository<AddressEntity>
{
    Task<IEnumerable<AddressEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<AddressEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<AddressEntity>> GetByNameAsync(string name);
}
