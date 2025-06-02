using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface IOrchestratedFlowEntityRepository : IBaseRepository<OrchestratedFlowEntity>
{
    Task<IEnumerable<OrchestratedFlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<OrchestratedFlowEntity>> GetByNameAsync(string name);
    Task<IEnumerable<OrchestratedFlowEntity>> GetByAssignmentIdAsync(Guid assignmentId);
    Task<IEnumerable<OrchestratedFlowEntity>> GetByFlowIdAsync(Guid flowId);
}
