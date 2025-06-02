using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class AddressEntityRepository : BaseRepository<AddressEntity>, IAddressEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public AddressEntityRepository(
        IMongoDatabase database,
        ILogger<AddressEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "addresses", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    public override async Task<AddressEntity> CreateAsync(AddressEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating AddressEntity. SchemaId: {SchemaId}", entity.SchemaId);

        try
        {
            // Validate SchemaId foreign key
            await _referentialIntegrityService.ValidateAddressEntityForeignKeysAsync(entity.SchemaId);

            _logger.LogInformation("Foreign key validation passed for AddressEntity. Proceeding with creation. SchemaId: {SchemaId}", entity.SchemaId);
            return await base.CreateAsync(entity);
        }
        catch (ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AddressEntity creation validation. SchemaId: {SchemaId}", entity.SchemaId);
            throw;
        }
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting AddressEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateAddressEntityDeletionAsync(id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of AddressEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.AddressEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for AddressEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AddressEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<AddressEntity> UpdateAsync(AddressEntity entity)
    {
        _logger.LogInformation("Validating referential integrity and foreign keys before updating AddressEntity {Id}. SchemaId: {SchemaId}", entity.Id, entity.SchemaId);

        try
        {
            // Validate SchemaId foreign key
            await _referentialIntegrityService.ValidateAddressEntityForeignKeysAsync(entity.SchemaId);

            // Validate referential integrity (existing logic)
            var validationResult = await _referentialIntegrityService.ValidateAddressEntityUpdateAsync(entity.Id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of AddressEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.AddressEntityReferences!);
            }

            _logger.LogInformation("Foreign key and referential integrity validation passed for AddressEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AddressEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override FilterDefinition<AddressEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 3);
        if (parts.Length != 3)
            throw new ArgumentException("Invalid composite key format for AddressEntity. Expected format: 'version_name_address'");

        return Builders<AddressEntity>.Filter.And(
            Builders<AddressEntity>.Filter.Eq(x => x.Version, parts[0]),
            Builders<AddressEntity>.Filter.Eq(x => x.Name, parts[1]),
            Builders<AddressEntity>.Filter.Eq(x => x.Address, parts[2])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (Version + Name + Address)
        var compositeKeyIndex = Builders<AddressEntity>.IndexKeys
            .Ascending(x => x.Version)
            .Ascending(x => x.Name)
            .Ascending(x => x.Address);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<AddressEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<AddressEntity>(
            Builders<AddressEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AddressEntity>(
            Builders<AddressEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AddressEntity>(
            Builders<AddressEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AddressEntity>(
            Builders<AddressEntity>.IndexKeys.Ascending(x => x.SchemaId)));
    }

    public async Task<IEnumerable<AddressEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<AddressEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<AddressEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<AddressEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<AddressEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<AddressEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(AddressEntity entity)
    {
        var createdEvent = new AddressCreatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Configuration = entity.Configuration,
            SchemaId = entity.SchemaId,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(AddressEntity entity)
    {
        var updatedEvent = new AddressUpdatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Configuration = entity.Configuration,
            SchemaId = entity.SchemaId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new AddressDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
