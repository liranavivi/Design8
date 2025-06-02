using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IFlowEntityRepository : IBaseRepository<FlowEntity>
{
    Task<IEnumerable<FlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<FlowEntity>> GetByNameAsync(string name);
    Task<IEnumerable<FlowEntity>> GetByStepIdAsync(Guid stepId);
}
