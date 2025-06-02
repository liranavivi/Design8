namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;

public class CreateDeliveryCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid SchemaId { get; set; } = Guid.Empty;
    public string Payload { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateDeliveryCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid SchemaId { get; set; } = Guid.Empty;
    public string Payload { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteDeliveryCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetDeliveryQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}
