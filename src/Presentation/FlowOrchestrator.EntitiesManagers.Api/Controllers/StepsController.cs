using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StepsController : ControllerBase
{
    private readonly IStepEntityRepository _repository;
    private readonly ILogger<StepsController> _logger;

    public StepsController(
        IStepEntityRepository repository,
        ILogger<StepsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StepEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll steps request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all step entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all step entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetPaged steps request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
            page, pageSize, userContext, HttpContext.TraceIdentifier);

        // Validate parameters - return 400 Bad Request for invalid values instead of auto-correcting
        if (page < 1)
        {
            _logger.LogWarning("Invalid page parameter provided: {Page}. User: {User}, RequestId: {RequestId}",
                page, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new
            {
                error = "Invalid page parameter",
                message = "Page must be greater than 0",
                parameter = "page",
                value = page
            });
        }

        if (pageSize < 1)
        {
            _logger.LogWarning("Invalid pageSize parameter provided: {PageSize}. User: {User}, RequestId: {RequestId}",
                pageSize, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new
            {
                error = "Invalid pageSize parameter",
                message = "PageSize must be greater than 0",
                parameter = "pageSize",
                value = pageSize
            });
        }

        if (pageSize > 100)
        {
            _logger.LogWarning("PageSize parameter exceeds maximum: {PageSize}. User: {User}, RequestId: {RequestId}",
                pageSize, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new
            {
                error = "Invalid pageSize parameter",
                message = "PageSize cannot exceed 100",
                parameter = "pageSize",
                value = pageSize,
                maximum = 100
            });
        }

        try
        {
            var entities = await _repository.GetPagedAsync(page, pageSize);
            var totalCount = await _repository.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation("Successfully retrieved paged step entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
                page, pageSize, entities.Count(), totalCount, totalPages, userContext, HttpContext.TraceIdentifier);

            return Ok(new
            {
                data = entities,
                page = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged step entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StepEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Step entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved step entity by ID. Id: {Id}, ProcessorId: {ProcessorId}, User: {User}, RequestId: {RequestId}",
                id, entity.ProcessorId, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the step entity");
        }
    }

    [HttpGet("by-key/{version}/{name}")]
    public async Task<ActionResult<StepEntity>> GetByCompositeKey(string version, string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        // StepEntity composite key format: "version_name"
        var compositeKey = $"{version}_{name}";

        _logger.LogInformation("Starting GetByCompositeKey step request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            version, name, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("Step entity not found by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with version '{version}' and name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved step entity by composite key. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entity by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the step entity");
        }
    }

    [HttpGet("by-processor-id/{processorId:guid}")]
    public async Task<ActionResult<IEnumerable<StepEntity>>> GetByProcessorId(Guid processorId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByProcessorId step request. ProcessorId: {ProcessorId}, User: {User}, RequestId: {RequestId}",
            processorId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByProcessorIdAsync(processorId);

            _logger.LogInformation("Successfully retrieved step entities by processor ID. ProcessorId: {ProcessorId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                processorId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entities by processor ID. ProcessorId: {ProcessorId}, User: {User}, RequestId: {RequestId}",
                processorId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpGet("by-next-step-id/{nextStepId:guid}")]
    public async Task<ActionResult<IEnumerable<StepEntity>>> GetByNextStepId(Guid nextStepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByNextStepId step request. NextStepId: {NextStepId}, User: {User}, RequestId: {RequestId}",
            nextStepId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNextStepIdAsync(nextStepId);

            _logger.LogInformation("Successfully retrieved step entities by next step ID. NextStepId: {NextStepId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                nextStepId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entities by next step ID. NextStepId: {NextStepId}, User: {User}, RequestId: {RequestId}",
                nextStepId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<StepEntity>> Create([FromBody] StepEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create step request. Version: {Version}, Name: {Name}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Version, entity?.Name, entity?.ProcessorId, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create step request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating step entity with details. Version: {Version}, Name: {Name}, ProcessorId: {ProcessorId}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.ProcessorId, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new StepEntity. ProcessorId: {ProcessorId}, User: {User}, RequestId: {RequestId}",
                    entity.ProcessorId, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created step entity. Id: {Id}, Version: {Version}, Name: {Name}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.ProcessorId, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during step creation. ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}], User: {User}, RequestId: {RequestId}",
                entity?.ProcessorId, entity?.NextStepIds != null ? string.Join(", ", entity.NextStepIds) : "null", userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = ex.GetApiErrorMessage(),
                errorCode = "FOREIGN_KEY_VALIDATION_FAILED",
                entityType = ex.EntityType,
                foreignKeyProperty = ex.ForeignKeyProperty,
                foreignKeyValue = ex.ForeignKeyValue,
                referencedEntityType = ex.ReferencedEntityType
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating step entity. ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.ProcessorId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating step entity. ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.ProcessorId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the step");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StepEntity>> Update(Guid id, [FromBody] StepEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update step request. Id: {Id}, Version: {Version}, Name: {Name}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Version, entity?.Name, entity?.ProcessorId, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update step request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update step request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Step entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with ID {id} not found");
            }

            _logger.LogDebug("Updating step entity. Id: {Id}, OldProcessorId: {OldProcessorId}, NewProcessorId: {NewProcessorId}, User: {User}, RequestId: {RequestId}",
                id, existing.ProcessorId, entity.ProcessorId, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated step entity. Id: {Id}, Version: {Version}, Name: {Name}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.ProcessorId, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during step update. Id: {Id}, ProcessorId: {ProcessorId}, NextStepIds: [{NextStepIds}], User: {User}, RequestId: {RequestId}",
                id, entity?.ProcessorId, entity?.NextStepIds != null ? string.Join(", ", entity.NextStepIds) : "null", userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = ex.GetApiErrorMessage(),
                errorCode = "FOREIGN_KEY_VALIDATION_FAILED",
                entityType = ex.EntityType,
                foreignKeyProperty = ex.ForeignKeyProperty,
                foreignKeyValue = ex.ForeignKeyValue,
                referencedEntityType = ex.ReferencedEntityType
            });
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented update of step entity. Id: {Id}, Error: {Error}, References: {FlowCount} flows, {AssignmentCount} assignments, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.StepEntityReferences?.FlowEntityCount ?? 0, ex.StepEntityReferences?.AssignmentEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    flowEntityCount = ex.StepEntityReferences?.FlowEntityCount ?? 0,
                    assignmentEntityCount = ex.StepEntityReferences?.AssignmentEntityCount ?? 0,
                    totalReferences = ex.StepEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.StepEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating step entity. Id: {Id}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.ProcessorId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Step entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Step with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating step entity. Id: {Id}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.ProcessorId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the step");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Step entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with ID {id} not found");
            }

            _logger.LogDebug("Deleting step entity. Id: {Id}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.ProcessorId, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete step entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the step entity");
            }

            _logger.LogInformation("Successfully deleted step entity. Id: {Id}, ProcessorId: {ProcessorId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.ProcessorId, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of step entity. Id: {Id}, Error: {Error}, References: {FlowCount} flows, {AssignmentCount} assignments, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.StepEntityReferences?.FlowEntityCount ?? 0, ex.StepEntityReferences?.AssignmentEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    flowEntityCount = ex.StepEntityReferences?.FlowEntityCount ?? 0,
                    assignmentEntityCount = ex.StepEntityReferences?.AssignmentEntityCount ?? 0,
                    totalReferences = ex.StepEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.StepEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting step entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the step");
        }
    }

    // ========================================
    // FALLBACK ROUTES FOR INVALID GUID FORMATS
    // ========================================
    // These routes handle cases where invalid GUID formats are provided
    // and return proper 400 Bad Request responses instead of 404 Not Found

    [HttpGet("{id}")]
    public ActionResult<StepEntity> GetByIdFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetById step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid ID format",
            message = "The provided ID is not a valid GUID format",
            parameter = "id",
            value = id,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpGet("by-processor-id/{processorId}")]
    public ActionResult<IEnumerable<StepEntity>> GetByProcessorIdFallback(string processorId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetByProcessorId step request. ProcessorId: {ProcessorId}, User: {User}, RequestId: {RequestId}",
            processorId, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid processorId format",
            message = "The provided processorId is not a valid GUID format",
            parameter = "processorId",
            value = processorId,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpGet("by-next-step-id/{nextStepId}")]
    public ActionResult<IEnumerable<StepEntity>> GetByNextStepIdFallback(string nextStepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetByNextStepId step request. NextStepId: {NextStepId}, User: {User}, RequestId: {RequestId}",
            nextStepId, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid nextStepId format",
            message = "The provided nextStepId is not a valid GUID format",
            parameter = "nextStepId",
            value = nextStepId,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpPut("{id}")]
    public ActionResult<StepEntity> UpdateFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Update step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid ID format",
            message = "The provided ID is not a valid GUID format",
            parameter = "id",
            value = id,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Delete step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid ID format",
            message = "The provided ID is not a valid GUID format",
            parameter = "id",
            value = id,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }
}
