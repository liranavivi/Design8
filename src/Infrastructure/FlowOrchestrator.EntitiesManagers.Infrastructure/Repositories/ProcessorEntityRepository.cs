using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;

public class ProcessorEntityRepository : BaseRepository<ProcessorEntity>, IProcessorEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public ProcessorEntityRepository(
        IMongoDatabase database,
        ILogger<ProcessorEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "processors", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<ProcessorEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // Parse composite key in format: "version_name"
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for ProcessorEntity. Expected format: 'version_name'");

        var version = parts[0];
        var name = parts[1];

        return Builders<ProcessorEntity>.Filter.And(
            Builders<ProcessorEntity>.Filter.Eq(x => x.Version, version),
            Builders<ProcessorEntity>.Filter.Eq(x => x.Name, name)
        );
    }

    protected override void CreateIndexes()
    {
        // Version index for uniqueness (since Version is now the composite key)
        var versionIndex = Builders<ProcessorEntity>.IndexKeys.Ascending(x => x.Version);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessorEntity>(versionIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessorEntity>(
            Builders<ProcessorEntity>.IndexKeys.Ascending(x => x.Name)));
    }

    // GetByAddressAsync method removed since ProcessorEntity no longer has Address property

    public override async Task<ProcessorEntity> CreateAsync(ProcessorEntity entity)
    {
        _logger.LogInformation("Validating foreign keys before creating ProcessorEntity. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
            entity.InputSchemaId, entity.OutputSchemaId);

        try
        {
            // Validate InputSchemaId and OutputSchemaId foreign keys
            await _referentialIntegrityService.ValidateProcessorEntityForeignKeysAsync(entity.InputSchemaId, entity.OutputSchemaId);

            _logger.LogInformation("Foreign key validation passed for ProcessorEntity. Proceeding with creation. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
                entity.InputSchemaId, entity.OutputSchemaId);

            return await base.CreateAsync(entity);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during foreign key validation for ProcessorEntity. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
                entity.InputSchemaId, entity.OutputSchemaId);
            throw;
        }
    }

    public async Task<IEnumerable<ProcessorEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ProcessorEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting ProcessorEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateProcessorEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of ProcessorEntity {Id}: {Error}. References: {StepCount} steps",
                    id, validationResult.ErrorMessage, validationResult.ProcessorEntityReferences?.StepEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.ProcessorEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for ProcessorEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProcessorEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<ProcessorEntity> UpdateAsync(ProcessorEntity entity)
    {
        _logger.LogInformation("Validating foreign keys and referential integrity before updating ProcessorEntity {Id}. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
            entity.Id, entity.InputSchemaId, entity.OutputSchemaId);

        try
        {
            // Validate InputSchemaId and OutputSchemaId foreign keys
            await _referentialIntegrityService.ValidateProcessorEntityForeignKeysAsync(entity.InputSchemaId, entity.OutputSchemaId);

            _logger.LogInformation("Foreign key validation passed for ProcessorEntity {Id}. Checking referential integrity", entity.Id);

            // Then validate referential integrity
            var validationResult = await _referentialIntegrityService.ValidateProcessorEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of ProcessorEntity {Id}: {Error}. References: {StepCount} steps",
                    entity.Id, validationResult.ErrorMessage, validationResult.ProcessorEntityReferences?.StepEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.ProcessorEntityReferences!);
            }

            _logger.LogInformation("All validations passed for ProcessorEntity {Id}. Proceeding with update", entity.Id);
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
            _logger.LogError(ex, "Error during validation for ProcessorEntity {Id}. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}",
                entity.Id, entity.InputSchemaId, entity.OutputSchemaId);
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(ProcessorEntity entity)
    {
        var createdEvent = new ProcessorCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            InputSchemaId = entity.InputSchemaId,
            OutputSchemaId = entity.OutputSchemaId,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ProcessorEntity entity)
    {
        var updatedEvent = new ProcessorUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            InputSchemaId = entity.InputSchemaId,
            OutputSchemaId = entity.OutputSchemaId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ProcessorDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
