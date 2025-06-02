namespace FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Commands;

public class CreateStepCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ProcessorId { get; set; } = Guid.Empty;
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateStepCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ProcessorId { get; set; } = Guid.Empty;
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteStepCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetStepQuery
{
    public Guid? Id { get; set; }
    public Guid? ProcessorId { get; set; }
}
