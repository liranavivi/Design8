using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;

public interface IReferentialIntegrityService
{



    // AddressEntity validation methods
    Task<ReferentialIntegrityResult> ValidateAddressEntityDeletionAsync(Guid addressId);
    Task<ReferentialIntegrityResult> ValidateAddressEntityUpdateAsync(Guid addressId);
    Task<AddressEntityReferenceInfo> GetAddressEntityReferencesAsync(Guid addressId);

    // Foreign key validation methods for CREATE/UPDATE operations
    Task ValidateAddressEntityForeignKeysAsync(Guid schemaId);
    Task ValidateDeliveryEntityForeignKeysAsync(Guid schemaId);
    Task ValidateProcessorEntityForeignKeysAsync(Guid inputSchemaId, Guid outputSchemaId);
    Task ValidateAssignmentEntityForeignKeysAsync(Guid stepId, List<Guid> entityIds);



    Task ValidateOrchestratedFlowEntityForeignKeysAsync(Guid flowId, List<Guid> assignmentIds);
    Task ValidateStepEntityForeignKeysAsync(Guid entityId, List<Guid> nextStepIds);
    Task ValidateFlowEntityForeignKeysAsync(List<Guid> stepIds);

    Task<ReferentialIntegrityResult> ValidateDeliveryEntityDeletionAsync(Guid deliveryId);
    Task<ReferentialIntegrityResult> ValidateDeliveryEntityUpdateAsync(Guid deliveryId);
    Task<DeliveryEntityReferenceInfo> GetDeliveryEntityReferencesAsync(Guid deliveryId);

    Task<ReferentialIntegrityResult> ValidateSchemaEntityDeletionAsync(Guid schemaId);
    Task<ReferentialIntegrityResult> ValidateSchemaEntityUpdateAsync(Guid schemaId);
    Task<SchemaEntityReferenceInfo> GetSchemaEntityReferencesAsync(Guid schemaId);





    Task<ReferentialIntegrityResult> ValidateProcessorEntityDeletionAsync(Guid processorId);
    Task<ReferentialIntegrityResult> ValidateProcessorEntityUpdateAsync(Guid processorId);
    Task<ProcessorEntityReferenceInfo> GetProcessorEntityReferencesAsync(Guid processorId);

    Task<ReferentialIntegrityResult> ValidateStepEntityDeletionAsync(Guid stepId);
    Task<ReferentialIntegrityResult> ValidateStepEntityUpdateAsync(Guid stepId);
    Task<StepEntityReferenceInfo> GetStepEntityReferencesAsync(Guid stepId);

    Task<ReferentialIntegrityResult> ValidateFlowEntityDeletionAsync(Guid flowId);
    Task<ReferentialIntegrityResult> ValidateFlowEntityUpdateAsync(Guid flowId);
    Task<FlowEntityReferenceInfo> GetFlowEntityReferencesAsync(Guid flowId);

    // OrchestratedFlowEntity validation methods
    Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityDeletionAsync(Guid orchestratedFlowId);
    Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityUpdateAsync(Guid orchestratedFlowId);
    Task<OrchestratedFlowEntityReferenceInfo> GetOrchestratedFlowEntityReferencesAsync(Guid orchestratedFlowId);

    // AssignmentEntity validation methods
    Task<ReferentialIntegrityResult> ValidateAssignmentEntityDeletionAsync(Guid assignmentId);
    Task<ReferentialIntegrityResult> ValidateAssignmentEntityUpdateAsync(Guid assignmentId);
    Task<AssignmentEntityReferenceInfo> GetAssignmentEntityReferencesAsync(Guid assignmentId);
}

public class ReferentialIntegrityResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    public AddressEntityReferenceInfo? AddressEntityReferences { get; set; }
    public DeliveryEntityReferenceInfo? DeliveryEntityReferences { get; set; }
    public SchemaEntityReferenceInfo? SchemaEntityReferences { get; set; }


    public ProcessorEntityReferenceInfo? ProcessorEntityReferences { get; private set; }
    public StepEntityReferenceInfo? StepEntityReferences { get; private set; }
    public FlowEntityReferenceInfo? FlowEntityReferences { get; private set; }
    public OrchestratedFlowEntityReferenceInfo? OrchestratedFlowEntityReferences { get; set; }
    public AssignmentEntityReferenceInfo? AssignmentEntityReferences { get; set; }
    public TimeSpan ValidationDuration { get; set; }

    public static ReferentialIntegrityResult Valid() => new() { IsValid = true };



    public static ReferentialIntegrityResult Invalid(string message, AddressEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        AddressEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, DeliveryEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        DeliveryEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, SchemaEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        SchemaEntityReferences = references
    };





    public static ReferentialIntegrityResult Invalid(string message, ProcessorEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ProcessorEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, StepEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        StepEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, FlowEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        FlowEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, OrchestratedFlowEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        OrchestratedFlowEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, AssignmentEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        AssignmentEntityReferences = references
    };
}



public class AddressEntityReferenceInfo
{
    public long AssignmentEntityCount { get; set; }
    public long TotalReferences => AssignmentEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (AssignmentEntityCount > 0) types.Add($"AssignmentEntity ({AssignmentEntityCount} records)");
        return types;
    }
}

public class DeliveryEntityReferenceInfo
{
    public long AssignmentEntityCount { get; set; }
    public long TotalReferences => AssignmentEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (AssignmentEntityCount > 0) types.Add($"AssignmentEntity ({AssignmentEntityCount} records)");
        return types;
    }
}

public class SchemaEntityReferenceInfo
{
    public long AssignmentEntityCount { get; set; }
    public long AddressEntityCount { get; set; }
    public long DeliveryEntityCount { get; set; }
    public long ProcessorEntityInputCount { get; set; }
    public long ProcessorEntityOutputCount { get; set; }

    public long TotalReferences => AssignmentEntityCount + AddressEntityCount + DeliveryEntityCount + ProcessorEntityInputCount + ProcessorEntityOutputCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (AssignmentEntityCount > 0) types.Add($"AssignmentEntity ({AssignmentEntityCount} records)");
        if (AddressEntityCount > 0) types.Add($"AddressEntity ({AddressEntityCount} records)");
        if (DeliveryEntityCount > 0) types.Add($"DeliveryEntity ({DeliveryEntityCount} records)");
        if (ProcessorEntityInputCount > 0) types.Add($"ProcessorEntity.InputSchemaId ({ProcessorEntityInputCount} records)");
        if (ProcessorEntityOutputCount > 0) types.Add($"ProcessorEntity.OutputSchemaId ({ProcessorEntityOutputCount} records)");
        return types;
    }
}





public class ProcessorEntityReferenceInfo
{
    public long StepEntityCount { get; set; }
    public long TotalReferences => StepEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (StepEntityCount > 0) types.Add($"StepEntity ({StepEntityCount} records)");
        return types;
    }
}

public class StepEntityReferenceInfo
{
    public long FlowEntityCount { get; set; }
    public long AssignmentEntityCount { get; set; }
    public long TotalReferences => FlowEntityCount + AssignmentEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (FlowEntityCount > 0) types.Add($"FlowEntity ({FlowEntityCount} records)");
        if (AssignmentEntityCount > 0) types.Add($"AssignmentEntity ({AssignmentEntityCount} records)");
        return types;
    }
}

public class FlowEntityReferenceInfo
{
    public long OrchestratedFlowEntityCount { get; set; }
    public long TotalReferences => OrchestratedFlowEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (OrchestratedFlowEntityCount > 0) types.Add($"OrchestratedFlowEntity ({OrchestratedFlowEntityCount} records)");
        return types;
    }
}

public class OrchestratedFlowEntityReferenceInfo
{
    public long TotalReferences => 0; // No references to validate anymore
    public bool HasReferences => false; 

    public List<string> GetReferencingEntityTypes()
    {
        return new List<string>();
    }
}

public class AssignmentEntityReferenceInfo
{
    public long OrchestratedFlowEntityCount { get; set; }
    public long TotalReferences => OrchestratedFlowEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (OrchestratedFlowEntityCount > 0) types.Add($"OrchestratedFlowEntity ({OrchestratedFlowEntityCount} records)");
        return types;
    }
}
