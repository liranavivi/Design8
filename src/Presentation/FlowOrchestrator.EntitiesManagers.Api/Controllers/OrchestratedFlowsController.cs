using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestratedFlowsController : ControllerBase
{
    private readonly IOrchestratedFlowEntityRepository _repository;
    private readonly ILogger<OrchestratedFlowsController> _logger;

    public OrchestratedFlowsController(
        IOrchestratedFlowEntityRepository repository,
        ILogger<OrchestratedFlowsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrchestratedFlowEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll orchestratedflows request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all orchestratedflow entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orchestratedflow entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving orchestratedflow entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetPaged orchestratedflows request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged orchestratedflow entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged orchestratedflow entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving orchestratedflow entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrchestratedFlowEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById orchestratedflow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("OrchestratedFlow entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"OrchestratedFlow with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved orchestratedflow entity by ID. Id: {Id}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestratedflow entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the orchestratedflow entity");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<OrchestratedFlowEntity> GetByIdFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetById orchestratedflow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid GUID format",
            message = $"The provided ID '{id}' is not a valid GUID format",
            parameter = "id",
            value = id,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpGet("by-assignment-id/{assignmentId:guid}")]
    public async Task<ActionResult<IEnumerable<OrchestratedFlowEntity>>> GetByAssignmentId(Guid assignmentId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByAssignmentId orchestratedflow request. AssignmentId: {AssignmentId}, User: {User}, RequestId: {RequestId}",
            assignmentId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByAssignmentIdAsync(assignmentId);

            _logger.LogInformation("Successfully retrieved orchestratedflow entities by assignment ID. AssignmentId: {AssignmentId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                assignmentId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestratedflow entities by assignment ID. AssignmentId: {AssignmentId}, User: {User}, RequestId: {RequestId}",
                assignmentId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving orchestratedflow entities");
        }
    }

    [HttpGet("by-assignment-id/{assignmentId}")]
    public ActionResult<IEnumerable<OrchestratedFlowEntity>> GetByAssignmentIdFallback(string assignmentId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetByAssignmentId orchestratedflow request. AssignmentId: {AssignmentId}, User: {User}, RequestId: {RequestId}",
            assignmentId, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid GUID format",
            message = $"The provided AssignmentId '{assignmentId}' is not a valid GUID format",
            parameter = "assignmentId",
            value = assignmentId,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpGet("by-flow-id/{flowId:guid}")]
    public async Task<ActionResult<IEnumerable<OrchestratedFlowEntity>>> GetByFlowId(Guid flowId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByFlowId orchestratedflow request. FlowId: {FlowId}, User: {User}, RequestId: {RequestId}",
            flowId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByFlowIdAsync(flowId);

            _logger.LogInformation("Successfully retrieved orchestratedflow entities by flow ID. FlowId: {FlowId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                flowId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestratedflow entities by flow ID. FlowId: {FlowId}, User: {User}, RequestId: {RequestId}",
                flowId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving orchestratedflow entities");
        }
    }

    [HttpGet("by-flow-id/{flowId}")]
    public ActionResult<IEnumerable<OrchestratedFlowEntity>> GetByFlowIdFallback(string flowId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetByFlowId orchestratedflow request. FlowId: {FlowId}, User: {User}, RequestId: {RequestId}",
            flowId, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid GUID format",
            message = $"The provided FlowId '{flowId}' is not a valid GUID format",
            parameter = "flowId",
            value = flowId,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<OrchestratedFlowEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName orchestratedflow request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved orchestratedflow entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestratedflow entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving orchestratedflow entities");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<OrchestratedFlowEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByVersion orchestratedflow request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved orchestratedflow entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestratedflow entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving orchestratedflow entities");
        }
    }

    [HttpGet("by-key/{version}/{name}")]
    public async Task<ActionResult<OrchestratedFlowEntity>> GetByCompositeKey(string version, string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        // OrchestratedFlowEntity composite key format: "version_name"
        var compositeKey = $"{version}_{name}";

        _logger.LogInformation("Starting GetByCompositeKey orchestratedflow request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            version, name, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);
            if (entity == null)
            {
                _logger.LogWarning("OrchestratedFlow entity not found by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"OrchestratedFlow with version '{version}' and name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved orchestratedflow entity by composite key. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestratedflow entity by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the orchestratedflow entity");
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrchestratedFlowEntity>> Create([FromBody] OrchestratedFlowEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create orchestratedflow request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create orchestratedflow request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating orchestratedflow entity with details. Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new OrchestratedFlowEntity. Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created orchestratedflow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during orchestratedflow creation. FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}], User: {User}, RequestId: {RequestId}",
                entity?.FlowId, entity?.AssignmentIds != null ? string.Join(", ", entity.AssignmentIds) : "null", userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning(ex, "Duplicate key conflict creating orchestratedflow entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating orchestratedflow entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the orchestratedflow");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrchestratedFlowEntity>> Update(Guid id, [FromBody] OrchestratedFlowEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update orchestratedflow request. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update orchestratedflow request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update orchestratedflow request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("OrchestratedFlow entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"OrchestratedFlow with ID {id} not found");
            }

            _logger.LogDebug("Updating orchestratedflow entity. Id: {Id}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated orchestratedflow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during orchestratedflow update. Id: {Id}, FlowId: {FlowId}, AssignmentIds: [{AssignmentIds}], User: {User}, RequestId: {RequestId}",
                id, entity?.FlowId, entity?.AssignmentIds != null ? string.Join(", ", entity.AssignmentIds) : "null", userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning("Referential integrity violation prevented update of orchestratedflow entity. Id: {Id}, Error: {Error}, User: {User}, RequestId: {RequestId}",
                id, ex.Message, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    totalReferences = ex.OrchestratedFlowEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.OrchestratedFlowEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating orchestratedflow entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("OrchestratedFlow entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"OrchestratedFlow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating orchestratedflow entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the orchestratedflow");
        }
    }

    [HttpPut("{id}")]
    public ActionResult<OrchestratedFlowEntity> UpdateFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Update orchestratedflow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid GUID format",
            message = $"The provided ID '{id}' is not a valid GUID format",
            parameter = "id",
            value = id,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete orchestratedflow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("OrchestratedFlow entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"OrchestratedFlow with ID {id} not found");
            }

            _logger.LogDebug("Deleting orchestratedflow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete orchestratedflow entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the orchestratedflow entity");
            }

            _logger.LogInformation("Successfully deleted orchestratedflow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of orchestratedflow entity. Id: {Id}, Error: {Error}, User: {User}, RequestId: {RequestId}",
                id, ex.Message, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    totalReferences = ex.OrchestratedFlowEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.OrchestratedFlowEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting orchestratedflow entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the orchestratedflow");
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Delete orchestratedflow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid GUID format",
            message = $"The provided ID '{id}' is not a valid GUID format",
            parameter = "id",
            value = id,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }
}
