﻿using FlowOrchestrator.EntitiesManagers.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace FlowOrchestrator.EntitiesManagers.Core.Entities;

public class AssignmentEntity : BaseEntity
{
    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("stepId")]
    [Required(ErrorMessage = "StepId is required")]
    public Guid StepId { get; set; }

    [BsonElement("entityIds")]
    [Required(ErrorMessage = "EntityIds are required")]
    public List<Guid> EntityIds { get; set; } = new List<Guid>();

    public override string GetCompositeKey() => $"{StepId}";
}
