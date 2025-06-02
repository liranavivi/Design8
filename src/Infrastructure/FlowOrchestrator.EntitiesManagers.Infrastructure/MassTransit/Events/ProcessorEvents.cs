namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;

public class ProcessorCreatedEvent
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid InputSchemaId { get; set; } = Guid.Empty;
    public Guid OutputSchemaId { get; set; } = Guid.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class ProcessorUpdatedEvent
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid InputSchemaId { get; set; } = Guid.Empty;
    public Guid OutputSchemaId { get; set; } = Guid.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class ProcessorDeletedEvent
{
    public Guid Id { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
