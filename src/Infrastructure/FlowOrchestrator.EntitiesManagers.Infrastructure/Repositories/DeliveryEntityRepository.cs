using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class DeliveryEntityRepository : BaseRepository<DeliveryEntity>, IDeliveryEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public DeliveryEntityRepository(
        IMongoDatabase database,
        ILogger<DeliveryEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "deliveries", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    public override async Task<DeliveryEntity> CreateAsync(DeliveryEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating DeliveryEntity. SchemaId: {SchemaId}", entity.SchemaId);

        try
        {
            // Validate SchemaId foreign key
            await _referentialIntegrityService.ValidateDeliveryEntityForeignKeysAsync(entity.SchemaId);

            _logger.LogInformation("Foreign key validation passed for DeliveryEntity. Proceeding with creation. SchemaId: {SchemaId}", entity.SchemaId);
            return await base.CreateAsync(entity);
        }
        catch (ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DeliveryEntity creation validation. SchemaId: {SchemaId}", entity.SchemaId);
            throw;
        }
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting DeliveryEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateDeliveryEntityDeletionAsync(id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of DeliveryEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.DeliveryEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for DeliveryEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DeliveryEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<DeliveryEntity> UpdateAsync(DeliveryEntity entity)
    {
        _logger.LogInformation("Validating foreign keys and referential integrity before updating DeliveryEntity {Id}. SchemaId: {SchemaId}", entity.Id, entity.SchemaId);

        try
        {
            // Validate SchemaId foreign key
            await _referentialIntegrityService.ValidateDeliveryEntityForeignKeysAsync(entity.SchemaId);

            // Validate referential integrity (existing logic)
            var validationResult = await _referentialIntegrityService.ValidateDeliveryEntityUpdateAsync(entity.Id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of DeliveryEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.DeliveryEntityReferences!);
            }

            _logger.LogInformation("Foreign key and referential integrity validation passed for DeliveryEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DeliveryEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override FilterDefinition<DeliveryEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for DeliveryEntity. Expected format: 'version_name'");

        return Builders<DeliveryEntity>.Filter.And(
            Builders<DeliveryEntity>.Filter.Eq(x => x.Version, parts[0]),
            Builders<DeliveryEntity>.Filter.Eq(x => x.Name, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (Version + Name)
        var compositeKeyIndex = Builders<DeliveryEntity>.IndexKeys
            .Ascending(x => x.Version)
            .Ascending(x => x.Name);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<DeliveryEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<DeliveryEntity>(
            Builders<DeliveryEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<DeliveryEntity>(
            Builders<DeliveryEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<DeliveryEntity>(
            Builders<DeliveryEntity>.IndexKeys.Ascending(x => x.SchemaId)));
    }

    public async Task<IEnumerable<DeliveryEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<DeliveryEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<DeliveryEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<DeliveryEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }



    protected override async Task PublishCreatedEventAsync(DeliveryEntity entity)
    {
        var createdEvent = new DeliveryCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            SchemaId = entity.SchemaId,
            Payload = entity.Payload,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(DeliveryEntity entity)
    {
        var updatedEvent = new DeliveryUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            SchemaId = entity.SchemaId,
            Payload = entity.Payload,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new DeliveryDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
