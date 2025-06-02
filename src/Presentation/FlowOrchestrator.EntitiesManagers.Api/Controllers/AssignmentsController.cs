using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentEntityRepository _repository;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(
        IAssignmentEntityRepository repository,
        ILogger<AssignmentsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssignmentEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll assignments request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all assignment entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all assignment entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving assignment entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged assignments request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
            page, pageSize, userContext, HttpContext.TraceIdentifier);

        // Strict parameter validation - return 400 for invalid parameters
        if (page < 1)
        {
            _logger.LogWarning("Invalid page parameter. Page: {Page}, User: {User}, RequestId: {RequestId}",
                page, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid page parameter",
                message = "Page must be greater than 0",
                parameter = "page",
                value = page
            });
        }

        if (pageSize < 1)
        {
            _logger.LogWarning("Invalid pageSize parameter (too small). PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                pageSize, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid pageSize parameter",
                message = "PageSize must be greater than 0",
                parameter = "pageSize",
                value = pageSize
            });
        }

        if (pageSize > 100)
        {
            _logger.LogWarning("Invalid pageSize parameter (too large). PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                pageSize, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
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

            _logger.LogInformation("Successfully retrieved paged assignment entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
                page, pageSize, entities.Count(), totalCount, totalPages, userContext, HttpContext.TraceIdentifier);

            return Ok(new
            {
                Data = entities,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged assignment entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving assignment entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssignmentEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById assignment request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Assignment entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Assignment with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved assignment entity by ID. Id: {Id}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the assignment entity");
        }
    }

    [HttpGet("by-key/{stepId:guid}")]
    public async Task<ActionResult<AssignmentEntity>> GetByCompositeKey(Guid stepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = stepId.ToString();

        _logger.LogInformation("Starting GetByCompositeKey assignment request. StepId: {StepId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            stepId, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("Assignment entity not found by composite key. StepId: {StepId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    stepId, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Assignment with stepId '{stepId}' not found");
            }

            _logger.LogInformation("Successfully retrieved assignment entity by composite key. Id: {Id}, StepId: {StepId}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, stepId, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment entity by composite key. StepId: {StepId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                stepId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the assignment entity");
        }
    }

    [HttpGet("by-step/{stepId:guid}")]
    public async Task<ActionResult<AssignmentEntity>> GetByStepId(Guid stepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByStepId assignment request. StepId: {StepId}, User: {User}, RequestId: {RequestId}",
            stepId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByStepIdAsync(stepId);

            if (entity == null)
            {
                _logger.LogWarning("Assignment entity not found by step ID. StepId: {StepId}, User: {User}, RequestId: {RequestId}",
                    stepId, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Assignment with stepId '{stepId}' not found");
            }

            _logger.LogInformation("Successfully retrieved assignment entity by step ID. StepId: {StepId}, Id: {Id}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                stepId, entity.Id, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment entity by step ID. StepId: {StepId}, User: {User}, RequestId: {RequestId}",
                stepId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving assignment entity");
        }
    }

    [HttpGet("by-entity/{entityId:guid}")]
    public async Task<ActionResult<IEnumerable<AssignmentEntity>>> GetByEntityId(Guid entityId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByEntityId assignment request. EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
            entityId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByEntityIdAsync(entityId);

            _logger.LogInformation("Successfully retrieved assignment entities by entity ID. EntityId: {EntityId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                entityId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment entities by entity ID. EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
                entityId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving assignment entities");
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<AssignmentEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        // Validate name parameter
        if (string.IsNullOrEmpty(name))
        {
            _logger.LogWarning("Empty name parameter in GetByName request. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid name parameter",
                message = "Name parameter cannot be null or empty",
                parameter = "name"
            });
        }

        _logger.LogInformation("Starting GetByName assignment request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved assignment entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving assignment entities");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<AssignmentEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        // Validate version parameter
        if (string.IsNullOrEmpty(version))
        {
            _logger.LogWarning("Empty version parameter in GetByVersion request. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid version parameter",
                message = "Version parameter cannot be null or empty",
                parameter = "version"
            });
        }

        _logger.LogInformation("Starting GetByVersion assignment request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved assignment entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving assignment entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<AssignmentEntity>> Create([FromBody] AssignmentEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create assignment request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create assignment request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating assignment entity with details. Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new AssignmentEntity. Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created assignment entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during assignment creation. StepId: {StepId}, EntityIds: [{EntityIds}], User: {User}, RequestId: {RequestId}",
                entity?.StepId, entity?.EntityIds != null ? string.Join(", ", entity.EntityIds) : "null", userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning(ex, "Duplicate key conflict creating assignment entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the assignment");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssignmentEntity>> Update(Guid id, [FromBody] AssignmentEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update assignment request. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update assignment request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update assignment request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Assignment entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Assignment with ID {id} not found");
            }

            _logger.LogDebug("Updating assignment entity. Id: {Id}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated assignment entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during assignment update. Id: {Id}, StepId: {StepId}, EntityIds: [{EntityIds}], User: {User}, RequestId: {RequestId}",
                id, entity?.StepId, entity?.EntityIds != null ? string.Join(", ", entity.EntityIds) : "null", userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning(ex, "Duplicate key conflict updating assignment entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Assignment entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Assignment with ID {id} not found");
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented update of assignment entity. Id: {Id}, Error: {Error}, References: {OrchestratedFlowCount} orchestrated flows, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.AssignmentEntityReferences?.OrchestratedFlowEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    orchestratedFlowEntityCount = ex.AssignmentEntityReferences?.OrchestratedFlowEntityCount ?? 0,
                    totalReferences = ex.AssignmentEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.AssignmentEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the assignment");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete assignment request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Assignment entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Assignment with ID {id} not found");
            }

            _logger.LogDebug("Deleting assignment entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete assignment entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the assignment entity");
            }

            _logger.LogInformation("Successfully deleted assignment entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of assignment entity. Id: {Id}, Error: {Error}, References: {OrchestratedFlowCount} orchestrated flows, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.AssignmentEntityReferences?.OrchestratedFlowEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    orchestratedFlowEntityCount = ex.AssignmentEntityReferences?.OrchestratedFlowEntityCount ?? 0,
                    totalReferences = ex.AssignmentEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.AssignmentEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the assignment");
        }
    }

    // ========================================
    // FALLBACK ROUTES FOR EMPTY PARAMETERS
    // ========================================
    // These routes handle cases where empty parameters are provided
    // and return proper 400 Bad Request responses instead of 404 Not Found

    [HttpGet("by-name")]
    public ActionResult<IEnumerable<AssignmentEntity>> GetByNameEmpty()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty name parameter in GetByName request (no parameter provided). User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        return BadRequest(new {
            error = "Invalid name parameter",
            message = "Name parameter cannot be null or empty",
            parameter = "name"
        });
    }

    [HttpGet("by-version")]
    public ActionResult<IEnumerable<AssignmentEntity>> GetByVersionEmpty()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty version parameter in GetByVersion request (no parameter provided). User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        return BadRequest(new {
            error = "Invalid version parameter",
            message = "Version parameter cannot be null or empty",
            parameter = "version"
        });
    }
}
