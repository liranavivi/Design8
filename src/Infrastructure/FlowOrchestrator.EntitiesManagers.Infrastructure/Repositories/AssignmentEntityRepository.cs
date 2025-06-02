using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class AssignmentEntityRepository : BaseRepository<AssignmentEntity>, IAssignmentEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public AssignmentEntityRepository(
        IMongoDatabase database,
        ILogger<AssignmentEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "assignments", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<AssignmentEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // StepId-only composite key
        if (!Guid.TryParse(compositeKey, out var stepId))
            throw new ArgumentException("Invalid composite key format for AssignmentEntity. Expected format: 'stepId' (GUID)");

        return Builders<AssignmentEntity>.Filter.Eq(x => x.StepId, stepId);
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (StepId-only)
        var compositeKeyIndex = Builders<AssignmentEntity>.IndexKeys
            .Ascending(x => x.StepId);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(
            Builders<AssignmentEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(
            Builders<AssignmentEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(
            Builders<AssignmentEntity>.IndexKeys.Ascending(x => x.EntityIds)));
    }

    public async Task<IEnumerable<AssignmentEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<AssignmentEntity?> GetByStepIdAsync(Guid stepId)
    {
        var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.StepId, stepId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AssignmentEntity>> GetByEntityIdAsync(Guid entityId)
    {
        var filter = Builders<AssignmentEntity>.Filter.AnyEq(x => x.EntityIds, entityId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<AssignmentEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<AssignmentEntity> CreateAsync(AssignmentEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating AssignmentEntity. StepId: {StepId}, EntityIds: [{EntityIds}]",
            entity.StepId, string.Join(", ", entity.EntityIds));

        try
        {
            // Validate foreign keys before creation
            await _referentialIntegrityService.ValidateAssignmentEntityForeignKeysAsync(entity.StepId, entity.EntityIds);

            _logger.LogInformation("Foreign key validation passed for AssignmentEntity. Proceeding with creation. StepId: {StepId}, EntityIds: [{EntityIds}]",
                entity.StepId, string.Join(", ", entity.EntityIds));

            return await base.CreateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during foreign key validation for AssignmentEntity. StepId: {StepId}, EntityIds: [{EntityIds}]",
                entity.StepId, string.Join(", ", entity.EntityIds));
            throw;
        }
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting AssignmentEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateAssignmentEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of AssignmentEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.AssignmentEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for AssignmentEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for AssignmentEntity {Id}", id);
            throw;
        }
    }

    public override async Task<AssignmentEntity> UpdateAsync(AssignmentEntity entity)
    {
        _logger.LogInformation("Validating foreign keys and referential integrity before updating AssignmentEntity {Id}. StepId: {StepId}, EntityIds: [{EntityIds}]",
            entity.Id, entity.StepId, string.Join(", ", entity.EntityIds));

        try
        {
            // First validate foreign keys
            await _referentialIntegrityService.ValidateAssignmentEntityForeignKeysAsync(entity.StepId, entity.EntityIds);

            _logger.LogInformation("Foreign key validation passed for AssignmentEntity {Id}. Checking referential integrity", entity.Id);

            // Then validate referential integrity
            var validationResult = await _referentialIntegrityService.ValidateAssignmentEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of AssignmentEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.AssignmentEntityReferences!);
            }

            _logger.LogInformation("All validations passed for AssignmentEntity {Id}. Proceeding with update", entity.Id);
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
            _logger.LogError(ex, "Error during validation for AssignmentEntity {Id}. StepId: {StepId}, EntityIds: [{EntityIds}]",
                entity.Id, entity.StepId, string.Join(", ", entity.EntityIds));
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(AssignmentEntity entity)
    {
        var createdEvent = new AssignmentCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepId = entity.StepId,
            EntityIds = entity.EntityIds,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(AssignmentEntity entity)
    {
        var updatedEvent = new AssignmentUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepId = entity.StepId,
            EntityIds = entity.EntityIds,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new AssignmentDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
