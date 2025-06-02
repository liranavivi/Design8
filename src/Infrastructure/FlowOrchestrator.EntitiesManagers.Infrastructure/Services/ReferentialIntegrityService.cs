using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;

namespace FlowOrchestrator.EntitiesManagers.Infrastructure.Services;

public class ReferentialIntegrityService : IReferentialIntegrityService
{
    private readonly IMongoDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReferentialIntegrityService> _logger;

    public ReferentialIntegrityService(
        IMongoDatabase database,
        IConfiguration configuration,
        ILogger<ReferentialIntegrityService> logger)
    {
        _database = database;
        _configuration = configuration;
        _logger = logger;
    }





    public async Task<ReferentialIntegrityResult> ValidateAddressEntityDeletionAsync(Guid addressId)
    {
        if (!IsValidationEnabled())
        {
            _logger.LogDebug("Referential integrity validation is disabled");
            return ReferentialIntegrityResult.Valid();
        }

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for AddressEntity {AddressId}", addressId);

        try
        {
            var references = await GetAddressEntityReferencesAsync(addressId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({AssignmentCount} assignments)",
                duration.TotalMilliseconds, references.TotalReferences, references.AssignmentEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete AddressEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for AddressEntity {AddressId}", addressId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateAddressEntityUpdateAsync(Guid addressId)
    {
        _logger.LogInformation("Validating AddressEntity update for {AddressId}", addressId);
        return await ValidateAddressEntityDeletionAsync(addressId); // Same validation logic
    }

    public async Task<AddressEntityReferenceInfo> GetAddressEntityReferencesAsync(Guid addressId)
    {
        var validateAssignments = bool.Parse(_configuration["ReferentialIntegrity:ValidateAssignmentReferences"] ?? "true");

        var references = new AddressEntityReferenceInfo();

        if (validateAssignments)
        {
            references.AssignmentEntityCount = await CountAssignmentAddressReferencesAsync(addressId);
        }

        return references;
    }

    private Task<long> CountScheduledFlowReferencesAsync(Guid sourceId)
    {
        try
        {
            // OrchestratedFlowEntity no longer has SourceId property - Assignment-focused architecture
            // Return 0 as there are no source references in the new architecture
            _logger.LogDebug("OrchestratedFlowEntity no longer references SourceId - Assignment-focused architecture. Returning 0 references for SourceId {SourceId}", sourceId);
            return Task.FromResult(0L);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for SourceId {SourceId}", sourceId);
            throw;
        }
    }

    private async Task<long> CountAssignmentAddressReferencesAsync(Guid addressId)
    {
        try
        {
            var collection = _database.GetCollection<AssignmentEntity>("assignments");
            var filter = Builders<AssignmentEntity>.Filter.AnyEq(x => x.EntityIds, addressId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} AssignmentEntity references for AddressId {AddressId}", count, addressId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting AssignmentEntity references for AddressId {AddressId}", addressId);
            throw;
        }
    }

    private async Task<long> CountAssignmentDeliveryReferencesAsync(Guid deliveryId)
    {
        try
        {
            var collection = _database.GetCollection<AssignmentEntity>("assignments");
            var filter = Builders<AssignmentEntity>.Filter.AnyEq(x => x.EntityIds, deliveryId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} AssignmentEntity references for DeliveryId {DeliveryId}", count, deliveryId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting AssignmentEntity references for DeliveryId {DeliveryId}", deliveryId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateDeliveryEntityDeletionAsync(Guid deliveryId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for DeliveryEntity {DeliveryId}", deliveryId);

        try
        {
            var references = await GetDeliveryEntityReferencesAsync(deliveryId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({AssignmentCount} assignments)",
                duration.TotalMilliseconds, references.TotalReferences, references.AssignmentEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete DeliveryEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for DeliveryEntity {DeliveryId}", deliveryId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateDeliveryEntityUpdateAsync(Guid deliveryId)
    {
        _logger.LogInformation("Validating DeliveryEntity update for {DeliveryId}", deliveryId);
        return await ValidateDeliveryEntityDeletionAsync(deliveryId); // Same validation logic
    }

    public async Task<DeliveryEntityReferenceInfo> GetDeliveryEntityReferencesAsync(Guid deliveryId)
    {
        var validateAssignments = bool.Parse(_configuration["ReferentialIntegrity:ValidateAssignmentReferences"] ?? "true");

        var references = new DeliveryEntityReferenceInfo();

        if (validateAssignments)
        {
            references.AssignmentEntityCount = await CountAssignmentDeliveryReferencesAsync(deliveryId);
        }

        return references;
    }

    public async Task<ReferentialIntegrityResult> ValidateSchemaEntityDeletionAsync(Guid schemaId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for SchemaEntity {SchemaId}", schemaId);

        try
        {
            var references = await GetSchemaEntityReferencesAsync(schemaId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({AssignmentCount} assignments, {AddressCount} addresses, {DeliveryCount} deliveries, {ProcessorInputCount} processor inputs, {ProcessorOutputCount} processor outputs)",
                duration.TotalMilliseconds, references.TotalReferences, references.AssignmentEntityCount, references.AddressEntityCount, references.DeliveryEntityCount, references.ProcessorEntityInputCount, references.ProcessorEntityOutputCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete SchemaEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for SchemaEntity {SchemaId}", schemaId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateSchemaEntityUpdateAsync(Guid schemaId)
    {
        _logger.LogInformation("Validating SchemaEntity update for {SchemaId}", schemaId);
        return await ValidateSchemaEntityDeletionAsync(schemaId); // Same validation logic
    }

    public async Task<SchemaEntityReferenceInfo> GetSchemaEntityReferencesAsync(Guid schemaId)
    {
        var validateAssignments = bool.Parse(_configuration["ReferentialIntegrity:ValidateAssignmentReferences"] ?? "true");
        var validateAddresses = bool.Parse(_configuration["ReferentialIntegrity:ValidateAddressReferences"] ?? "true");
        var validateDeliveries = bool.Parse(_configuration["ReferentialIntegrity:ValidateDeliveryReferences"] ?? "true");
        var validateProcessors = bool.Parse(_configuration["ReferentialIntegrity:ValidateProcessorReferences"] ?? "true");

        var references = new SchemaEntityReferenceInfo();

        if (validateAssignments)
        {
            references.AssignmentEntityCount = await CountAssignmentSchemaReferencesAsync(schemaId);
        }

        if (validateAddresses)
        {
            references.AddressEntityCount = await CountAddressSchemaReferencesAsync(schemaId);
        }

        if (validateDeliveries)
        {
            references.DeliveryEntityCount = await CountDeliverySchemaReferencesAsync(schemaId);
        }

        if (validateProcessors)
        {
            references.ProcessorEntityInputCount = await CountProcessorInputSchemaReferencesAsync(schemaId);
            references.ProcessorEntityOutputCount = await CountProcessorOutputSchemaReferencesAsync(schemaId);
        }

        return references;
    }

    private async Task<long> CountAssignmentSchemaReferencesAsync(Guid schemaId)
    {
        try
        {
            var collection = _database.GetCollection<AssignmentEntity>("assignments");
            var filter = Builders<AssignmentEntity>.Filter.AnyEq(x => x.EntityIds, schemaId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} AssignmentEntity references for SchemaId {SchemaId}", count, schemaId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting AssignmentEntity references for SchemaId {SchemaId}", schemaId);
            throw;
        }
    }

    private async Task<long> CountAddressSchemaReferencesAsync(Guid schemaId)
    {
        try
        {
            var collection = _database.GetCollection<AddressEntity>("addresses");
            var filter = Builders<AddressEntity>.Filter.Eq(x => x.SchemaId, schemaId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} AddressEntity references for SchemaId {SchemaId}", count, schemaId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting AddressEntity references for SchemaId {SchemaId}", schemaId);
            throw;
        }
    }

    private async Task<long> CountDeliverySchemaReferencesAsync(Guid schemaId)
    {
        try
        {
            var collection = _database.GetCollection<DeliveryEntity>("deliveries");
            var filter = Builders<DeliveryEntity>.Filter.Eq(x => x.SchemaId, schemaId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} DeliveryEntity references for SchemaId {SchemaId}", count, schemaId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting DeliveryEntity references for SchemaId {SchemaId}", schemaId);
            throw;
        }
    }

    private async Task<long> CountProcessorInputSchemaReferencesAsync(Guid schemaId)
    {
        try
        {
            var collection = _database.GetCollection<ProcessorEntity>("processors");
            var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.InputSchemaId, schemaId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} ProcessorEntity.InputSchemaId references for SchemaId {SchemaId}", count, schemaId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting ProcessorEntity.InputSchemaId references for SchemaId {SchemaId}", schemaId);
            throw;
        }
    }

    private async Task<long> CountProcessorOutputSchemaReferencesAsync(Guid schemaId)
    {
        try
        {
            var collection = _database.GetCollection<ProcessorEntity>("processors");
            var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.OutputSchemaId, schemaId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} ProcessorEntity.OutputSchemaId references for SchemaId {SchemaId}", count, schemaId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting ProcessorEntity.OutputSchemaId references for SchemaId {SchemaId}", schemaId);
            throw;
        }
    }

    private Task<long> CountScheduledFlowDestinationReferencesAsync(Guid destinationId)
    {
        try
        {
            // OrchestratedFlowEntity no longer has DestinationIds property - Assignment-focused architecture
            // Return 0 as there are no destination references in the new architecture
            _logger.LogDebug("OrchestratedFlowEntity no longer references DestinationIds - Assignment-focused architecture. Returning 0 references for DestinationId {DestinationId}", destinationId);
            return Task.FromResult(0L);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for DestinationId {DestinationId}", destinationId);
            throw;
        }
    }





    public async Task<ReferentialIntegrityResult> ValidateProcessorEntityDeletionAsync(Guid processorId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for ProcessorEntity {ProcessorId}", processorId);

        try
        {
            var references = await GetProcessorEntityReferencesAsync(processorId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({StepCount} steps)",
                duration.TotalMilliseconds, references.TotalReferences, references.StepEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete ProcessorEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for ProcessorEntity {ProcessorId}", processorId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateProcessorEntityUpdateAsync(Guid processorId)
    {
        _logger.LogInformation("Validating ProcessorEntity update for {ProcessorId}", processorId);
        return await ValidateProcessorEntityDeletionAsync(processorId); // Same validation logic
    }

    public async Task<ProcessorEntityReferenceInfo> GetProcessorEntityReferencesAsync(Guid processorId)
    {
        var validateSteps = bool.Parse(_configuration["ReferentialIntegrity:ValidateStepReferences"] ?? "true");

        var references = new ProcessorEntityReferenceInfo();

        if (validateSteps)
        {
            references.StepEntityCount = await CountStepProcessorReferencesAsync(processorId);
        }

        return references;
    }

    private async Task<long> CountStepProcessorReferencesAsync(Guid processorId)
    {
        try
        {
            var collection = _database.GetCollection<StepEntity>("steps");
            var filter = Builders<StepEntity>.Filter.Eq(x => x.ProcessorId, processorId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} StepEntity references for ProcessorId {ProcessorId}", count, processorId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting StepEntity references for ProcessorId {ProcessorId}", processorId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateStepEntityDeletionAsync(Guid stepId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for StepEntity {StepId}", stepId);

        try
        {
            var references = await GetStepEntityReferencesAsync(stepId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({FlowCount} flows, {AssignmentCount} assignments)",
                duration.TotalMilliseconds, references.TotalReferences, references.FlowEntityCount, references.AssignmentEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete StepEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for StepEntity {StepId}", stepId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateStepEntityUpdateAsync(Guid stepId)
    {
        _logger.LogInformation("Validating StepEntity update for {StepId}", stepId);
        return await ValidateStepEntityDeletionAsync(stepId); // Same validation logic
    }

    public async Task<StepEntityReferenceInfo> GetStepEntityReferencesAsync(Guid stepId)
    {
        var validateFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateFlowReferences"] ?? "true");
        var validateAssignments = bool.Parse(_configuration["ReferentialIntegrity:ValidateAssignmentReferences"] ?? "true");

        var references = new StepEntityReferenceInfo();

        if (validateFlows)
        {
            references.FlowEntityCount = await CountFlowStepReferencesAsync(stepId);
        }

        if (validateAssignments)
        {
            references.AssignmentEntityCount = await CountAssignmentStepReferencesAsync(stepId);
        }

        return references;
    }

    private async Task<long> CountFlowStepReferencesAsync(Guid stepId)
    {
        try
        {
            var collection = _database.GetCollection<FlowEntity>("flows");
            var filter = Builders<FlowEntity>.Filter.AnyEq(x => x.StepIds, stepId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} FlowEntity references for StepId {StepId}", count, stepId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting FlowEntity references for StepId {StepId}", stepId);
            throw;
        }
    }

    private async Task<long> CountAssignmentStepReferencesAsync(Guid stepId)
    {
        try
        {
            var collection = _database.GetCollection<AssignmentEntity>("assignments");
            var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.StepId, stepId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} AssignmentEntity references for StepId {StepId}", count, stepId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting AssignmentEntity references for StepId {StepId}", stepId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateFlowEntityDeletionAsync(Guid flowId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for FlowEntity {FlowId}", flowId);

        try
        {
            var references = await GetFlowEntityReferencesAsync(flowId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({OrchestratedFlowCount} orchestrated flows)",
                duration.TotalMilliseconds, references.TotalReferences, references.OrchestratedFlowEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete FlowEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for FlowEntity {FlowId}", flowId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateFlowEntityUpdateAsync(Guid flowId)
    {
        _logger.LogInformation("Validating FlowEntity update for {FlowId}", flowId);
        return await ValidateFlowEntityDeletionAsync(flowId); // Same validation logic
    }

    public async Task<FlowEntityReferenceInfo> GetFlowEntityReferencesAsync(Guid flowId)
    {
        var validateScheduledFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateScheduledFlowReferences"] ?? "true");

        var references = new FlowEntityReferenceInfo();

        if (validateScheduledFlows)
        {
            references.OrchestratedFlowEntityCount = await CountOrchestratedFlowFlowReferencesAsync(flowId);
        }

        return references;
    }

    private async Task<long> CountOrchestratedFlowFlowReferencesAsync(Guid flowId)
    {
        try
        {
            var collection = _database.GetCollection<OrchestratedFlowEntity>("orchestratedflows");
            var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.FlowId, flowId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} OrchestratedFlowEntity references for FlowId {FlowId}", count, flowId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for FlowId {FlowId}", flowId);
            throw;
        }
    }

    // OrchestratedFlowEntity validation methods
    public async Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityDeletionAsync(Guid orchestratedFlowId)
    {
        _logger.LogInformation("Validating OrchestratedFlowEntity deletion for {OrchestratedFlowId}", orchestratedFlowId);
        var startTime = DateTime.UtcNow;

        try
        {
            var references = await GetOrchestratedFlowEntityReferencesAsync(orchestratedFlowId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references",
                duration.TotalMilliseconds, references.TotalReferences);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete OrchestratedFlowEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for OrchestratedFlowEntity {OrchestratedFlowId}", orchestratedFlowId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityUpdateAsync(Guid orchestratedFlowId)
    {
        _logger.LogInformation("Validating OrchestratedFlowEntity update for {OrchestratedFlowId}", orchestratedFlowId);
        return await ValidateOrchestratedFlowEntityDeletionAsync(orchestratedFlowId); // Same validation logic
    }

    public Task<OrchestratedFlowEntityReferenceInfo> GetOrchestratedFlowEntityReferencesAsync(Guid orchestratedFlowId)
    {
        // TaskScheduledEntity removed - no longer need to validate TaskScheduled references
        var references = new OrchestratedFlowEntityReferenceInfo();

        // Note: TaskScheduledEntityCount removed from OrchestratedFlowEntityReferenceInfo
        // OrchestratedFlowEntity now only manages AssignmentIds relationships

        return Task.FromResult(references);
    }

    public async Task<ReferentialIntegrityResult> ValidateAssignmentEntityDeletionAsync(Guid assignmentId)
    {
        _logger.LogInformation("Starting referential integrity validation for AssignmentEntity {AssignmentId}", assignmentId);
        var startTime = DateTime.UtcNow;

        try
        {
            var references = await GetAssignmentEntityReferencesAsync(assignmentId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({OrchestratedFlowCount} orchestrated flows)",
                duration.TotalMilliseconds, references.TotalReferences, references.OrchestratedFlowEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete AssignmentEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for AssignmentEntity {AssignmentId}", assignmentId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateAssignmentEntityUpdateAsync(Guid assignmentId)
    {
        _logger.LogInformation("Validating AssignmentEntity update for {AssignmentId}", assignmentId);
        return await ValidateAssignmentEntityDeletionAsync(assignmentId); // Same validation logic
    }

    public async Task<AssignmentEntityReferenceInfo> GetAssignmentEntityReferencesAsync(Guid assignmentId)
    {
        var validateOrchestratedFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateOrchestratedFlowReferences"] ?? "true");

        var references = new AssignmentEntityReferenceInfo();

        if (validateOrchestratedFlows)
        {
            references.OrchestratedFlowEntityCount = await CountOrchestratedFlowAssignmentReferencesAsync(assignmentId);
        }

        return references;
    }

    private async Task<long> CountOrchestratedFlowAssignmentReferencesAsync(Guid assignmentId)
    {
        try
        {
            var collection = _database.GetCollection<OrchestratedFlowEntity>("orchestratedflows");
            var filter = Builders<OrchestratedFlowEntity>.Filter.AnyEq(x => x.AssignmentIds, assignmentId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} OrchestratedFlowEntity references for AssignmentId {AssignmentId}", count, assignmentId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for AssignmentId {AssignmentId}", assignmentId);
            throw;
        }
    }

    private bool IsValidationEnabled()
    {
        return bool.Parse(_configuration["Features:ReferentialIntegrityValidation"] ?? "true");
    }

    // Foreign key validation methods for CREATE/UPDATE operations

    private async Task<bool> ValidateFlowExistsAsync(Guid flowId)
    {
        try
        {
            var collection = _database.GetCollection<FlowEntity>("flows");
            var filter = Builders<FlowEntity>.Filter.Eq(x => x.Id, flowId);
            var count = await collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });

            var exists = count > 0;
            _logger.LogDebug("FlowEntity existence check for {FlowId}: {Exists}", flowId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating FlowEntity existence for {FlowId}", flowId);
            throw;
        }
    }

    private async Task<bool> ValidateAssignmentExistsAsync(Guid assignmentId)
    {
        try
        {
            var collection = _database.GetCollection<AssignmentEntity>("assignments");
            var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.Id, assignmentId);
            var count = await collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });

            var exists = count > 0;
            _logger.LogDebug("AssignmentEntity existence check for {AssignmentId}: {Exists}", assignmentId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating AssignmentEntity existence for {AssignmentId}", assignmentId);
            throw;
        }
    }

    private async Task<bool> ValidateOperationalEntityExistsAsync(Guid entityId)
    {
        try
        {




            // Check ProcessorEntity collection
            var processorCollection = _database.GetCollection<ProcessorEntity>("processors");
            var processorFilter = Builders<ProcessorEntity>.Filter.Eq(x => x.Id, entityId);
            var processorCount = await processorCollection.CountDocumentsAsync(processorFilter, new CountOptions { Limit = 1 });
            if (processorCount > 0)
            {
                _logger.LogDebug("Operational entity existence check for {EntityId}: Found in ProcessorEntity", entityId);
                return true;
            }

            _logger.LogDebug("Operational entity existence check for {EntityId}: Not found in any collection", entityId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating operational entity existence for {EntityId}", entityId);
            throw;
        }
    }

    private async Task<bool> ValidateStepExistsAsync(Guid stepId)
    {
        try
        {
            var collection = _database.GetCollection<StepEntity>("steps");
            var filter = Builders<StepEntity>.Filter.Eq(x => x.Id, stepId);
            var count = await collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });

            var exists = count > 0;
            _logger.LogDebug("StepEntity existence check for {StepId}: {Exists}", stepId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating StepEntity existence for {StepId}", stepId);
            throw;
        }
    }

    public async Task ValidateAddressEntityForeignKeysAsync(Guid schemaId)
    {
        _logger.LogInformation("Validating foreign keys for AddressEntity. SchemaId: {SchemaId}", schemaId);

        try
        {
            // Validate SchemaId exists
            var schemaCollection = _database.GetCollection<SchemaEntity>("schemas");
            var schemaExists = await schemaCollection.CountDocumentsAsync(
                Builders<SchemaEntity>.Filter.Eq(s => s.Id, schemaId),
                new CountOptions { Limit = 1 }) > 0;

            if (!schemaExists)
            {
                _logger.LogWarning("Foreign key validation failed for AddressEntity. SchemaEntity with ID {SchemaId} does not exist", schemaId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "AddressEntity",
                    "SchemaId",
                    schemaId,
                    "SchemaEntity");
            }

            _logger.LogInformation("Foreign key validation passed for AddressEntity. SchemaId: {SchemaId}", schemaId);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for AddressEntity. SchemaId: {SchemaId}", schemaId);
            throw;
        }
    }

    public async Task ValidateDeliveryEntityForeignKeysAsync(Guid schemaId)
    {
        _logger.LogInformation("Validating foreign keys for DeliveryEntity. SchemaId: {SchemaId}", schemaId);

        try
        {
            // Validate SchemaId exists
            var schemaCollection = _database.GetCollection<SchemaEntity>("schemas");
            var schemaExists = await schemaCollection.CountDocumentsAsync(
                Builders<SchemaEntity>.Filter.Eq(s => s.Id, schemaId),
                new CountOptions { Limit = 1 }) > 0;

            if (!schemaExists)
            {
                _logger.LogWarning("Foreign key validation failed for DeliveryEntity. SchemaEntity with ID {SchemaId} does not exist", schemaId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "DeliveryEntity",
                    "SchemaId",
                    schemaId,
                    "SchemaEntity");
            }

            _logger.LogInformation("Foreign key validation passed for DeliveryEntity. SchemaId: {SchemaId}", schemaId);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for DeliveryEntity. SchemaId: {SchemaId}", schemaId);
            throw;
        }
    }

    public async Task ValidateProcessorEntityForeignKeysAsync(Guid inputSchemaId, Guid outputSchemaId)
    {
        _logger.LogInformation("Validating foreign keys for ProcessorEntity. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}", inputSchemaId, outputSchemaId);

        try
        {
            var schemaCollection = _database.GetCollection<SchemaEntity>("schemas");

            // Validate InputSchemaId exists
            var inputSchemaExists = await schemaCollection.CountDocumentsAsync(
                Builders<SchemaEntity>.Filter.Eq(s => s.Id, inputSchemaId),
                new CountOptions { Limit = 1 }) > 0;

            if (!inputSchemaExists)
            {
                _logger.LogWarning("Foreign key validation failed for ProcessorEntity. InputSchemaEntity with ID {InputSchemaId} does not exist", inputSchemaId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "ProcessorEntity",
                    "InputSchemaId",
                    inputSchemaId,
                    "SchemaEntity");
            }

            // Validate OutputSchemaId exists
            var outputSchemaExists = await schemaCollection.CountDocumentsAsync(
                Builders<SchemaEntity>.Filter.Eq(s => s.Id, outputSchemaId),
                new CountOptions { Limit = 1 }) > 0;

            if (!outputSchemaExists)
            {
                _logger.LogWarning("Foreign key validation failed for ProcessorEntity. OutputSchemaEntity with ID {OutputSchemaId} does not exist", outputSchemaId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "ProcessorEntity",
                    "OutputSchemaId",
                    outputSchemaId,
                    "SchemaEntity");
            }

            _logger.LogInformation("Foreign key validation passed for ProcessorEntity. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}", inputSchemaId, outputSchemaId);
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for ProcessorEntity. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}", inputSchemaId, outputSchemaId);
            throw;
        }
    }

    public async Task ValidateAssignmentEntityForeignKeysAsync(Guid stepId, List<Guid> entityIds)
    {
        _logger.LogInformation("Validating foreign keys for AssignmentEntity. StepId: {StepId}, EntityIds: [{EntityIds}]",
            stepId, string.Join(", ", entityIds));

        try
        {
            // Validate StepId exists
            var stepCollection = _database.GetCollection<StepEntity>("steps");
            var stepExists = await stepCollection.CountDocumentsAsync(
                Builders<StepEntity>.Filter.Eq(s => s.Id, stepId),
                new CountOptions { Limit = 1 }) > 0;

            if (!stepExists)
            {
                _logger.LogWarning("Foreign key validation failed for AssignmentEntity. StepEntity with ID {StepId} does not exist", stepId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "AssignmentEntity",
                    "StepId",
                    stepId,
                    "StepEntity");
            }

            // Validate all EntityIds exist in their respective collections
            foreach (var entityId in entityIds)
            {
                var entityExists = await ValidateEntityExistsInAnyCollectionAsync(entityId);
                if (!entityExists)
                {
                    _logger.LogWarning("Foreign key validation failed for AssignmentEntity. Entity with ID {EntityId} does not exist in any operational entity collection", entityId);
                    throw new Core.Exceptions.ForeignKeyValidationException(
                        "AssignmentEntity",
                        "EntityIds",
                        entityId,
                        "OperationalEntity");
                }
            }

            _logger.LogInformation("Foreign key validation passed for AssignmentEntity. StepId: {StepId}, EntityIds: [{EntityIds}]",
                stepId, string.Join(", ", entityIds));
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for AssignmentEntity. StepId: {StepId}, EntityIds: [{EntityIds}]",
                stepId, string.Join(", ", entityIds));
            throw;
        }
    }







    public async Task ValidateOrchestratedFlowEntityForeignKeysAsync(Guid flowId, List<Guid> assignmentIds)
    {
        _logger.LogInformation("Validating foreign keys for OrchestratedFlowEntity. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
            flowId, string.Join(", ", assignmentIds));

        try
        {
            // Validate FlowId exists
            var flowExists = await ValidateFlowExistsAsync(flowId);
            if (!flowExists)
            {
                _logger.LogWarning("Foreign key validation failed for OrchestratedFlowEntity. FlowEntity with ID {FlowId} does not exist", flowId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "OrchestratedFlowEntity",
                    "FlowId",
                    flowId,
                    "FlowEntity");
            }

            // Validate all AssignmentIds exist
            foreach (var assignmentId in assignmentIds)
            {
                var assignmentExists = await ValidateAssignmentExistsAsync(assignmentId);
                if (!assignmentExists)
                {
                    _logger.LogWarning("Foreign key validation failed for OrchestratedFlowEntity. AssignmentEntity with ID {AssignmentId} does not exist", assignmentId);
                    throw new Core.Exceptions.ForeignKeyValidationException(
                        "OrchestratedFlowEntity",
                        "AssignmentIds",
                        assignmentId,
                        "AssignmentEntity");
                }
            }

            _logger.LogInformation("Foreign key validation passed for OrchestratedFlowEntity. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
                flowId, string.Join(", ", assignmentIds));
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for OrchestratedFlowEntity. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}]",
                flowId, string.Join(", ", assignmentIds));
            throw;
        }
    }

    public async Task ValidateStepEntityForeignKeysAsync(Guid entityId, List<Guid> nextStepIds)
    {
        _logger.LogInformation("Validating foreign keys for StepEntity. EntityId: {EntityId}, NextStepIds: [{NextStepIds}]",
            entityId, string.Join(", ", nextStepIds));

        try
        {
            // Validate EntityId exists (can be ProcessorEntity)
            var entityExists = await ValidateOperationalEntityExistsAsync(entityId);
            if (!entityExists)
            {
                _logger.LogWarning("Foreign key validation failed for StepEntity. Operational entity with ID {EntityId} does not exist", entityId);
                throw new Core.Exceptions.ForeignKeyValidationException(
                    "StepEntity",
                    "EntityId",
                    entityId,
                    "ProcessorEntity");
            }

            // Validate all NextStepIds exist
            foreach (var nextStepId in nextStepIds)
            {
                var stepExists = await ValidateStepExistsAsync(nextStepId);
                if (!stepExists)
                {
                    _logger.LogWarning("Foreign key validation failed for StepEntity. StepEntity with ID {NextStepId} does not exist", nextStepId);
                    throw new Core.Exceptions.ForeignKeyValidationException(
                        "StepEntity",
                        "NextStepIds",
                        nextStepId,
                        "StepEntity");
                }
            }

            _logger.LogInformation("Foreign key validation passed for StepEntity. EntityId: {EntityId}, NextStepIds: [{NextStepIds}]",
                entityId, string.Join(", ", nextStepIds));
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for StepEntity. EntityId: {EntityId}, NextStepIds: [{NextStepIds}]",
                entityId, string.Join(", ", nextStepIds));
            throw;
        }
    }

    public async Task ValidateFlowEntityForeignKeysAsync(List<Guid> stepIds)
    {
        _logger.LogInformation("Validating foreign keys for FlowEntity. StepIds: [{StepIds}]",
            string.Join(", ", stepIds));

        try
        {
            // Validate all StepIds exist
            foreach (var stepId in stepIds)
            {
                var stepExists = await ValidateStepExistsAsync(stepId);
                if (!stepExists)
                {
                    _logger.LogWarning("Foreign key validation failed for FlowEntity. StepEntity with ID {StepId} does not exist", stepId);
                    throw new Core.Exceptions.ForeignKeyValidationException(
                        "FlowEntity",
                        "StepIds",
                        stepId,
                        "StepEntity");
                }
            }

            _logger.LogInformation("Foreign key validation passed for FlowEntity. StepIds: [{StepIds}]",
                string.Join(", ", stepIds));
        }
        catch (Core.Exceptions.ForeignKeyValidationException)
        {
            throw; // Re-throw foreign key validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys for FlowEntity. StepIds: [{StepIds}]",
                string.Join(", ", stepIds));
            throw;
        }
    }

    private async Task<bool> ValidateEntityExistsInAnyCollectionAsync(Guid entityId)
    {
        // Check if entity exists in any of the operational entity collections
        var tasks = new List<Task<bool>>
        {
            // Check AddressEntity collection
            _database.GetCollection<AddressEntity>("addresses").CountDocumentsAsync(
                Builders<AddressEntity>.Filter.Eq(a => a.Id, entityId),
                new CountOptions { Limit = 1 }).ContinueWith(t => t.Result > 0),





            // Check ProcessorEntity collection
            _database.GetCollection<ProcessorEntity>("processors").CountDocumentsAsync(
                Builders<ProcessorEntity>.Filter.Eq(p => p.Id, entityId),
                new CountOptions { Limit = 1 }).ContinueWith(t => t.Result > 0),

            // Check DeliveryEntity collection
            _database.GetCollection<DeliveryEntity>("deliveries").CountDocumentsAsync(
                Builders<DeliveryEntity>.Filter.Eq(d => d.Id, entityId),
                new CountOptions { Limit = 1 }).ContinueWith(t => t.Result > 0)
        };

        var results = await Task.WhenAll(tasks);
        return results.Any(exists => exists);
    }
}
