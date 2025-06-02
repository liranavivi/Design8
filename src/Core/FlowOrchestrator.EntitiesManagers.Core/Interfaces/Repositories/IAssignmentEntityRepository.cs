using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IAssignmentEntityRepository : IBaseRepository<AssignmentEntity>
{
    Task<IEnumerable<AssignmentEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<AssignmentEntity>> GetByNameAsync(string name);
    Task<AssignmentEntity?> GetByStepIdAsync(Guid stepId);
    Task<IEnumerable<AssignmentEntity>> GetByEntityIdAsync(Guid entityId);
}
