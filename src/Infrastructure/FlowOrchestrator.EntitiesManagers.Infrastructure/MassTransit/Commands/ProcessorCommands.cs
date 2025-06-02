namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;

public class CreateProcessorCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid InputSchemaId { get; set; } = Guid.Empty;
    public Guid OutputSchemaId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateProcessorCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid InputSchemaId { get; set; } = Guid.Empty;
    public Guid OutputSchemaId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteProcessorCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetProcessorQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}
