namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;

public class CreateSchemaCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateSchemaCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteSchemaCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetSchemaQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

public class GetSchemaDefinitionQuery
{
    public Guid SchemaId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetSchemaDefinitionQueryResponse
{
    public bool Success { get; set; }
    public string? Definition { get; set; }
    public string Message { get; set; } = string.Empty;
}
