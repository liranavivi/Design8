namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;

public class CreateAddressCommand
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid SchemaId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateAddressCommand
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid SchemaId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteAddressCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetAddressQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}
