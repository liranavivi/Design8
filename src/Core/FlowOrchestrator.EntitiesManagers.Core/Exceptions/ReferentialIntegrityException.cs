using System;
using System.Collections.Generic;
using System.Linq;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;

namespace FlowOrchestrator.EntitiesManagers.Core.Exceptions;

public class ReferentialIntegrityException : Exception
{

    public AddressEntityReferenceInfo? AddressEntityReferences { get; }
    public DeliveryEntityReferenceInfo? DeliveryEntityReferences { get; }
    public SchemaEntityReferenceInfo? SchemaEntityReferences { get; }


    public ProcessorEntityReferenceInfo? ProcessorEntityReferences { get; }
    public StepEntityReferenceInfo? StepEntityReferences { get; }
    public FlowEntityReferenceInfo? FlowEntityReferences { get; }
    public OrchestratedFlowEntityReferenceInfo? OrchestratedFlowEntityReferences { get; }
    public AssignmentEntityReferenceInfo? AssignmentEntityReferences { get; }



    // Constructor for AddressEntity validation
    public ReferentialIntegrityException(string message, AddressEntityReferenceInfo references)
        : base(message)
    {
        AddressEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, AddressEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        AddressEntityReferences = references;
    }

    // Constructor for DeliveryEntity validation
    public ReferentialIntegrityException(string message, DeliveryEntityReferenceInfo references)
        : base(message)
    {
        DeliveryEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, DeliveryEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        DeliveryEntityReferences = references;
    }

    // Constructor for SchemaEntity validation
    public ReferentialIntegrityException(string message, SchemaEntityReferenceInfo references)
        : base(message)
    {
        SchemaEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, SchemaEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        SchemaEntityReferences = references;
    }





    // Constructor for ProcessorEntity validation
    public ReferentialIntegrityException(string message, ProcessorEntityReferenceInfo references)
        : base(message)
    {
        ProcessorEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, ProcessorEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        ProcessorEntityReferences = references;
    }

    // Constructor for StepEntity validation
    public ReferentialIntegrityException(string message, StepEntityReferenceInfo references)
        : base(message)
    {
        StepEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, StepEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        StepEntityReferences = references;
    }

    // Constructor for FlowEntity validation
    public ReferentialIntegrityException(string message, FlowEntityReferenceInfo references)
        : base(message)
    {
        FlowEntityReferences = references;
    }

    // Constructor for OrchestratedFlowEntity validation
    public ReferentialIntegrityException(string message, OrchestratedFlowEntityReferenceInfo references)
        : base(message)
    {
        OrchestratedFlowEntityReferences = references;
    }

    // Constructor for AssignmentEntity validation
    public ReferentialIntegrityException(string message, AssignmentEntityReferenceInfo references)
        : base(message)
    {
        AssignmentEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, AssignmentEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        AssignmentEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, FlowEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        FlowEntityReferences = references;
    }

    public string GetDetailedMessage()
    {
        // Handle AssignmentEntity references
        if (AssignmentEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify AssignmentEntity. Found {AssignmentEntityReferences.OrchestratedFlowEntityCount} OrchestratedFlowEntity reference{(AssignmentEntityReferences.OrchestratedFlowEntityCount > 1 ? "s" : "")}.";
        }

        // Handle OrchestratedFlowEntity references
        if (OrchestratedFlowEntityReferences?.HasReferences == true)
        {
            // TaskScheduledEntity removed - OrchestratedFlowEntity no longer has references to validate
            return $"Cannot modify OrchestratedFlowEntity. Found references.";
        }

        // Handle FlowEntity references
        if (FlowEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify FlowEntity. Found {FlowEntityReferences.OrchestratedFlowEntityCount} OrchestratedFlowEntity reference{(FlowEntityReferences.OrchestratedFlowEntityCount > 1 ? "s" : "")}.";
        }

        // Handle StepEntity references
        if (StepEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify StepEntity. Found {StepEntityReferences.FlowEntityCount} FlowEntity reference{(StepEntityReferences.FlowEntityCount > 1 ? "s" : "")}.";
        }

        // Handle ProcessorEntity references
        if (ProcessorEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify ProcessorEntity. Found {ProcessorEntityReferences.StepEntityCount} StepEntity reference{(ProcessorEntityReferences.StepEntityCount > 1 ? "s" : "")}.";
        }





        // Handle DeliveryEntity references
        if (DeliveryEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify DeliveryEntity. Found references.";
        }

        // Handle SchemaEntity references
        if (SchemaEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify SchemaEntity. Found references.";
        }

        // Handle AddressEntity references
        if (AddressEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify AddressEntity. Found references.";
        }



        return Message;
    }
}
