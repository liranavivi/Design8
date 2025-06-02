using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories.Base;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;

public interface ISchemaEntityRepository : IBaseRepository<SchemaEntity>
{
    Task<IEnumerable<SchemaEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<SchemaEntity>> GetByNameAsync(string name);
    Task<IEnumerable<SchemaEntity>> GetByDefinitionAsync(string definition);
}
