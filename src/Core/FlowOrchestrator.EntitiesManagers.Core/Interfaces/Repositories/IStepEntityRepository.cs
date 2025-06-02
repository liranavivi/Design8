using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IStepEntityRepository : IBaseRepository<StepEntity>
{
    Task<IEnumerable<StepEntity>> GetByProcessorIdAsync(Guid processorId);
    Task<IEnumerable<StepEntity>> GetByNextStepIdAsync(Guid nextStepId);
}
