using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace FlowOrchestrator.EntitiesManagers.Core.Entities;

public class DeliveryEntity : BaseEntity
{
    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("schemaId")]
    [Required(ErrorMessage = "SchemaId is required")]
    public Guid SchemaId { get; set; } = Guid.Empty;

    [BsonElement("payload")]
    [Required(ErrorMessage = "Payload is required")]
    public string Payload { get; set; } = string.Empty;

    public override string GetCompositeKey() => $"{Version}_{Name}";
}
