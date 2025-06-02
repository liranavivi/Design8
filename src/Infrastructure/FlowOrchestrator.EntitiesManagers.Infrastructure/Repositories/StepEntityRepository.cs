using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class StepEntityRepository : BaseRepository<StepEntity>, IStepEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public StepEntityRepository(
        IMongoDatabase database,
        ILogger<StepEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "steps", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<StepEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // Parse composite key in format: "version_name"
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for StepEntity. Expected format: 'version_name'");

        var version = parts[0];
        var name = parts[1];

        return Builders<StepEntity>.Filter.And(
            Builders<StepEntity>.Filter.Eq(x => x.Version, version),
            Builders<StepEntity>.Filter.Eq(x => x.Name, name)
        );
    }

    protected override void CreateIndexes()
    {
        try
        {
            // Composite key index for uniqueness (Version + Name)
            var compositeKeyIndex = Builders<StepEntity>.IndexKeys
                .Ascending(x => x.Version)
                .Ascending(x => x.Name);

            _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
                compositeKeyIndex,
                new CreateIndexOptions
                {
                    Name = "step_composite_key_idx",
                    Unique = true
                }));

            // Individual indexes for common queries
            _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
                Builders<StepEntity>.IndexKeys.Ascending(x => x.Version),
                new CreateIndexOptions { Name = "step_version_idx" }));

            _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
                Builders<StepEntity>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Name = "step_name_idx" }));

            // ProcessorId index for workflow processor references
            _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
                Builders<StepEntity>.IndexKeys.Ascending(x => x.ProcessorId),
                new CreateIndexOptions { Name = "step_processorid_idx" }));

            // NextStepIds index for workflow navigation queries
            _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
                Builders<StepEntity>.IndexKeys.Ascending(x => x.NextStepIds),
                new CreateIndexOptions { Name = "step_nextstepids_idx" }));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index") || ex.Message.Contains("different name"))
        {
            _logger.LogInformation("Index already exists for StepEntity (possibly with different name), skipping creation: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indexes for StepEntity");
            throw;
        }
    }

    // GetByAddressAsync, GetByVersionAsync, and GetByNameAsync methods removed
    // since StepEntity no longer has these properties

    public async Task<IEnumerable<StepEntity>> GetByProcessorIdAsync(Guid processorId)
    {
        var filter = Builders<StepEntity>.Filter.Eq(x => x.ProcessorId, processorId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<StepEntity>> GetByNextStepIdAsync(Guid nextStepId)
    {
        var filter = Builders<StepEntity>.Filter.AnyEq(x => x.NextStepIds, nextStepId);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<StepEntity> CreateAsync(StepEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating StepEntity. ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}]",
            entity.ProcessorId, string.Join(", ", entity.NextStepIds));

        try
        {
            // Validate foreign keys before creation
            await _referentialIntegrityService.ValidateStepEntityForeignKeysAsync(entity.ProcessorId, entity.NextStepIds);

            _logger.LogInformation("Foreign key validation passed for StepEntity. Proceeding with creation. ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}]",
                entity.ProcessorId, string.Join(", ", entity.NextStepIds));

            return await base.CreateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during foreign key validation for StepEntity. ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}]",
                entity.ProcessorId, string.Join(", ", entity.NextStepIds));
            throw;
        }
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting StepEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateStepEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of StepEntity {Id}: {Error}. References: {FlowCount} flows, {AssignmentCount} assignments",
                    id, validationResult.ErrorMessage, validationResult.StepEntityReferences?.FlowEntityCount ?? 0, validationResult.StepEntityReferences?.AssignmentEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.StepEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for StepEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during StepEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<StepEntity> UpdateAsync(StepEntity entity)
    {
        _logger.LogInformation("Validating foreign keys and referential integrity before updating StepEntity {Id}. ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}]",
            entity.Id, entity.ProcessorId, string.Join(", ", entity.NextStepIds));

        try
        {
            // First validate foreign keys
            await _referentialIntegrityService.ValidateStepEntityForeignKeysAsync(entity.ProcessorId, entity.NextStepIds);

            _logger.LogInformation("Foreign key validation passed for StepEntity {Id}. Checking referential integrity", entity.Id);

            // Then validate referential integrity
            var validationResult = await _referentialIntegrityService.ValidateStepEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of StepEntity {Id}: {Error}. References: {FlowCount} flows, {AssignmentCount} assignments",
                    entity.Id, validationResult.ErrorMessage, validationResult.StepEntityReferences?.FlowEntityCount ?? 0, validationResult.StepEntityReferences?.AssignmentEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.StepEntityReferences!);
            }

            _logger.LogInformation("All validations passed for StepEntity {Id}. Proceeding with update", entity.Id);
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
            _logger.LogError(ex, "Error during validation for StepEntity {Id}. ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}]",
                entity.Id, entity.ProcessorId, string.Join(", ", entity.NextStepIds));
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(StepEntity entity)
    {
        var createdEvent = new StepCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            ProcessorId = entity.ProcessorId,
            NextStepIds = entity.NextStepIds,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(StepEntity entity)
    {
        var updatedEvent = new StepUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            ProcessorId = entity.ProcessorId,
            NextStepIds = entity.NextStepIds,
            Description = entity.Description,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new StepDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
