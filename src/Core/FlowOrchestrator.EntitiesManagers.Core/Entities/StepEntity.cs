using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace FlowOrchestrator.EntitiesManagers.Core.Entities;

public class StepEntity : BaseEntity
{
    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("processorId")]
    [Required(ErrorMessage = "ProcessorId is required")]
    public Guid ProcessorId { get; set; } = Guid.Empty;

    [BsonElement("nextStepIds")]
    [Required(ErrorMessage = "NextStepIds are required")]
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();

    public override string GetCompositeKey() => $"{Version}_{Name}";
}
