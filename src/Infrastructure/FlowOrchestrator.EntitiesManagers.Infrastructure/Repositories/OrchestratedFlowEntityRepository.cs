using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class OrchestratedFlowEntityRepository : BaseRepository<OrchestratedFlowEntity>, IOrchestratedFlowEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public OrchestratedFlowEntityRepository(
        IMongoDatabase database,
        ILogger<OrchestratedFlowEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "orchestratedflows", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<OrchestratedFlowEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // Parse composite key in format: "version_name"
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for OrchestratedFlowEntity. Expected format: 'version_name'");

        var version = parts[0];
        var name = parts[1];

        return Builders<OrchestratedFlowEntity>.Filter.And(
            Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.Version, version),
            Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.Name, name)
        );
    }

    protected override void CreateIndexes()
    {
        try
        {
            // Composite key index for uniqueness (Version + Name)
            var compositeKeyIndex = Builders<OrchestratedFlowEntity>.IndexKeys
                .Ascending(x => x.Version)
                .Ascending(x => x.Name);

            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                compositeKeyIndex,
                new CreateIndexOptions
                {
                    Name = "orchestratedflow_composite_key_idx",
                    Unique = true
                }));

            // Individual indexes for common queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.Version),
                new CreateIndexOptions { Name = "orchestratedflow_version_idx" }));

            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Name = "orchestratedflow_name_idx" }));

            // AssignmentIds index for assignment-based queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.AssignmentIds),
                new CreateIndexOptions { Name = "orchestratedflow_assignmentids_idx" }));

            // FlowId index for flow-based queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.FlowId),
                new CreateIndexOptions { Name = "orchestratedflow_flowid_idx" }));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index") || ex.Message.Contains("different name"))
        {
            _logger.LogInformation("Index already exists for OrchestratedFlowEntity (possibly with different name), skipping creation: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indexes for OrchestratedFlowEntity");
            throw;
        }
    }

    // GetByAddressAsync method removed since OrchestratedFlowEntity no longer has Address property

    public override async Task<OrchestratedFlowEntity> CreateAsync(OrchestratedFlowEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating OrchestratedFlowEntity. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
            entity.FlowId, string.Join(", ", entity.AssignmentIds));

        try
        {
            // Validate foreign keys before creation
            await _referentialIntegrityService.ValidateOrchestratedFlowEntityForeignKeysAsync(entity.FlowId, entity.AssignmentIds);

            _logger.LogInformation("Foreign key validation passed for OrchestratedFlowEntity. Proceeding with creation. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
                entity.FlowId, string.Join(", ", entity.AssignmentIds));

            return await base.CreateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during foreign key validation for OrchestratedFlowEntity. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
                entity.FlowId, string.Join(", ", entity.AssignmentIds));
            throw;
        }
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Starting referential integrity validation for OrchestratedFlowEntity deletion. Id: {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateOrchestratedFlowEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of OrchestratedFlowEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.OrchestratedFlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for OrchestratedFlowEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for OrchestratedFlowEntity {Id}", id);
            throw;
        }
    }

    public override async Task<OrchestratedFlowEntity> UpdateAsync(OrchestratedFlowEntity entity)
    {
        _logger.LogInformation("Validating foreign keys and referential integrity before updating OrchestratedFlowEntity {Id}. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
            entity.Id, entity.FlowId, string.Join(", ", entity.AssignmentIds));

        try
        {
            // First validate foreign keys
            await _referentialIntegrityService.ValidateOrchestratedFlowEntityForeignKeysAsync(entity.FlowId, entity.AssignmentIds);

            _logger.LogInformation("Foreign key validation passed for OrchestratedFlowEntity {Id}. Checking referential integrity", entity.Id);

            // Then validate referential integrity
            var validationResult = await _referentialIntegrityService.ValidateOrchestratedFlowEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of OrchestratedFlowEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.OrchestratedFlowEntityReferences!);
            }

            _logger.LogInformation("All validations passed for OrchestratedFlowEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation for OrchestratedFlowEntity {Id}. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
                entity.Id, entity.FlowId, string.Join(", ", entity.AssignmentIds));
            throw;
        }
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByAssignmentIdAsync(Guid assignmentId)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.AnyEq(x => x.AssignmentIds, assignmentId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByFlowIdAsync(Guid flowId)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.FlowId, flowId);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(OrchestratedFlowEntity entity)
    {
        var createdEvent = new OrchestratedFlowCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            AssignmentIds = entity.AssignmentIds,
            FlowId = entity.FlowId,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(OrchestratedFlowEntity entity)
    {
        var updatedEvent = new OrchestratedFlowUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            AssignmentIds = entity.AssignmentIds,
            FlowId = entity.FlowId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new OrchestratedFlowDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
