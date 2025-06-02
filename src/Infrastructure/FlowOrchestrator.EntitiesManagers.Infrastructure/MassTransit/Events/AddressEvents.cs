namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Events;

public class AddressCreatedEvent
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid SchemaId { get; set; } = Guid.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class AddressUpdatedEvent
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid SchemaId { get; set; } = Guid.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class AddressDeletedEvent
{
    public Guid Id { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
