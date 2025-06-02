using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class SchemaEntityRepository : BaseRepository<SchemaEntity>, ISchemaEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public SchemaEntityRepository(
        IMongoDatabase database,
        ILogger<SchemaEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "schemas", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting SchemaEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateSchemaEntityDeletionAsync(id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of SchemaEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.SchemaEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for SchemaEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SchemaEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<SchemaEntity> UpdateAsync(SchemaEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating SchemaEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateSchemaEntityUpdateAsync(entity.Id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of SchemaEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.SchemaEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for SchemaEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SchemaEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override FilterDefinition<SchemaEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for SchemaEntity. Expected format: 'version_name'");

        return Builders<SchemaEntity>.Filter.And(
            Builders<SchemaEntity>.Filter.Eq(x => x.Version, parts[0]),
            Builders<SchemaEntity>.Filter.Eq(x => x.Name, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (Version + Name)
        var compositeKeyIndex = Builders<SchemaEntity>.IndexKeys
            .Ascending(x => x.Version)
            .Ascending(x => x.Name);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<SchemaEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<SchemaEntity>(
            Builders<SchemaEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SchemaEntity>(
            Builders<SchemaEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SchemaEntity>(
            Builders<SchemaEntity>.IndexKeys.Ascending(x => x.Definition)));
    }

    public async Task<IEnumerable<SchemaEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<SchemaEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<SchemaEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<SchemaEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<SchemaEntity>> GetByDefinitionAsync(string definition)
    {
        var filter = Builders<SchemaEntity>.Filter.Eq(x => x.Definition, definition);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(SchemaEntity entity)
    {
        var createdEvent = new SchemaCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Definition = entity.Definition,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(SchemaEntity entity)
    {
        var updatedEvent = new SchemaUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Definition = entity.Definition,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new SchemaDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
