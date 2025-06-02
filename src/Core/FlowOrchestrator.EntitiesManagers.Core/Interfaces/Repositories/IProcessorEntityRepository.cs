using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IProcessorEntityRepository : IBaseRepository<ProcessorEntity>
{
    Task<IEnumerable<ProcessorEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ProcessorEntity>> GetByNameAsync(string name);
}
