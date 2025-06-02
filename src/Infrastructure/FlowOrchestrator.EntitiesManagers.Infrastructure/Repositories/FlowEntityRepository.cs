using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class FlowEntityRepository : BaseRepository<FlowEntity>, IFlowEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public FlowEntityRepository(
        IMongoDatabase database,
        ILogger<FlowEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "flows", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<FlowEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // Parse composite key in format: "version_name"
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for FlowEntity. Expected format: 'version_name'");

        var version = parts[0];
        var name = parts[1];

        return Builders<FlowEntity>.Filter.And(
            Builders<FlowEntity>.Filter.Eq(x => x.Version, version),
            Builders<FlowEntity>.Filter.Eq(x => x.Name, name)
        );
    }

    protected override void CreateIndexes()
    {
        try
        {
            // Composite key index for uniqueness (Version + Name)
            var compositeKeyIndex = Builders<FlowEntity>.IndexKeys
                .Ascending(x => x.Version)
                .Ascending(x => x.Name);

            _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
                compositeKeyIndex,
                new CreateIndexOptions
                {
                    Name = "flow_composite_key_idx",
                    Unique = true
                }));

            // Individual indexes for common queries
            _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
                Builders<FlowEntity>.IndexKeys.Ascending(x => x.Version),
                new CreateIndexOptions { Name = "flow_version_idx" }));

            _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
                Builders<FlowEntity>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Name = "flow_name_idx" }));

            // StepIds index for workflow step references
            _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
                Builders<FlowEntity>.IndexKeys.Ascending(x => x.StepIds),
                new CreateIndexOptions { Name = "flow_stepids_idx" }));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index") || ex.Message.Contains("different name"))
        {
            _logger.LogInformation("Index already exists for FlowEntity (possibly with different name), skipping creation: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indexes for FlowEntity");
            throw;
        }
    }

    // GetByAddressAsync method removed since FlowEntity no longer has Address property

    public async Task<IEnumerable<FlowEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<FlowEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FlowEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<FlowEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FlowEntity>> GetByStepIdAsync(Guid stepId)
    {
        var filter = Builders<FlowEntity>.Filter.AnyEq(x => x.StepIds, stepId);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting FlowEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateFlowEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of FlowEntity {Id}: {Error}. References: {OrchestratedFlowCount} orchestrated flows",
                    id, validationResult.ErrorMessage, validationResult.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.FlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for FlowEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FlowEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<FlowEntity> CreateAsync(FlowEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating FlowEntity");

        try
        {
            // Validate foreign key references (StepIds)
            await _referentialIntegrityService.ValidateFlowEntityForeignKeysAsync(entity.StepIds);

            _logger.LogInformation("Foreign key validation passed for FlowEntity. Proceeding with creation");
            return await base.CreateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException ex)
        {
            _logger.LogWarning("Foreign key validation failed for FlowEntity creation: {Error}", ex.Message);
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FlowEntity creation validation");
            throw;
        }
    }

    public override async Task<FlowEntity> UpdateAsync(FlowEntity entity)
    {
        _logger.LogInformation("Validating referential integrity and foreign keys before updating FlowEntity {Id}", entity.Id);

        try
        {
            // Validate foreign key references (StepIds)
            await _referentialIntegrityService.ValidateFlowEntityForeignKeysAsync(entity.StepIds);

            var validationResult = await _referentialIntegrityService.ValidateFlowEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of FlowEntity {Id}: {Error}. References: {OrchestratedFlowCount} orchestrated flows",
                    entity.Id, validationResult.ErrorMessage, validationResult.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.FlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity and foreign key validation passed for FlowEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException ex)
        {
            _logger.LogWarning("Foreign key validation failed for FlowEntity update: {Error}", ex.Message);
            throw; // Re-throw foreign key validation exceptions
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FlowEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(FlowEntity entity)
    {
        var createdEvent = new FlowCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepIds = entity.StepIds,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(FlowEntity entity)
    {
        var updatedEvent = new FlowUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepIds = entity.StepIds,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new FlowDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
