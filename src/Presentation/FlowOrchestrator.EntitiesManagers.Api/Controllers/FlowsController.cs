using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowsController : ControllerBase
{
    private readonly IFlowEntityRepository _repository;
    private readonly ILogger<FlowsController> _logger;

    public FlowsController(
        IFlowEntityRepository repository,
        ILogger<FlowsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll flows request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all flow entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all flow entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetPaged flows request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged flow entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged flow entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<FlowEntity> GetByIdFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetById flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FlowEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Flow entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved flow entity by ID. Id: {Id}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the flow entity");
        }
    }

    [HttpGet("by-step-id/{stepId}")]
    public ActionResult<IEnumerable<FlowEntity>> GetByStepIdFallback(string stepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetByStepId flow request. StepId: {StepId}, User: {User}, RequestId: {RequestId}",
            stepId, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Invalid stepId format",
            message = "The provided stepId is not a valid GUID format",
            parameter = "stepId",
            value = stepId,
            expectedFormat = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        });
    }

    [HttpGet("by-step-id/{stepId:guid}")]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetByStepId(Guid stepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByStepId flow request. StepId: {StepId}, User: {User}, RequestId: {RequestId}",
            stepId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByStepIdAsync(stepId);

            _logger.LogInformation("Successfully retrieved flow entities by step ID. StepId: {StepId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                stepId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entities by step ID. StepId: {StepId}, User: {User}, RequestId: {RequestId}",
                stepId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("by-name")]
    public ActionResult<IEnumerable<FlowEntity>> GetByNameFallback()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty name parameter in GetByName flow request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Missing name parameter",
            message = "The name parameter is required",
            parameter = "name",
            expectedFormat = "/api/flows/by-name/{name}"
        });
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName flow request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved flow entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("by-version")]
    public ActionResult<IEnumerable<FlowEntity>> GetByVersionFallback()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty version parameter in GetByVersion flow request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Missing version parameter",
            message = "The version parameter is required",
            parameter = "version",
            expectedFormat = "/api/flows/by-version/{version}"
        });
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByVersion flow request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved flow entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("by-key")]
    public ActionResult<FlowEntity> GetByCompositeKeyFallback()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty composite key parameters in GetByCompositeKey flow request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Missing composite key parameters",
            message = "Both version and name parameters are required",
            parameters = new[] { "version", "name" },
            expectedFormat = "/api/flows/by-key/{version}/{name}"
        });
    }

    [HttpGet("by-key/{version}")]
    public ActionResult<FlowEntity> GetByCompositeKeyVersionOnlyFallback(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Missing name parameter in GetByCompositeKey flow request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new
        {
            error = "Missing name parameter",
            message = "The name parameter is required",
            parameter = "name",
            providedVersion = version,
            expectedFormat = "/api/flows/by-key/{version}/{name}"
        });
    }

    [HttpGet("by-key/{version}/{name}")]
    public async Task<ActionResult<FlowEntity>> GetByCompositeKey(string version, string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        // FlowEntity composite key format: "version_name"
        var compositeKey = $"{version}_{name}";

        _logger.LogInformation("Starting GetByCompositeKey flow request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            version, name, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);
            if (entity == null)
            {
                _logger.LogWarning("Flow entity not found by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with version '{version}' and name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved flow entity by composite key. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entity by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the flow entity");
        }
    }

    [HttpPost]
    public async Task<ActionResult<FlowEntity>> Create([FromBody] FlowEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create flow request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create flow request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating flow entity with details. Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new FlowEntity. Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created flow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Core.Exceptions.ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed creating flow entity. Entity: {Entity}, Property: {Property}, Value: {Value}, ReferencedEntity: {ReferencedEntity}, User: {User}, RequestId: {RequestId}",
                ex.EntityType, ex.ForeignKeyProperty, ex.ForeignKeyValue, ex.ReferencedEntityType, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new
            {
                message = ex.Message,
                apiMessage = ex.GetApiErrorMessage(),
                entity = ex.EntityType,
                property = ex.ForeignKeyProperty,
                value = ex.ForeignKeyValue,
                referencedEntity = ex.ReferencedEntityType
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating flow entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flow entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the flow");
        }
    }

    [HttpPut("{id}")]
    public ActionResult<FlowEntity> UpdateFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Update flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FlowEntity>> Update(Guid id, [FromBody] FlowEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update flow request. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update flow request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update flow request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Flow entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with ID {id} not found");
            }

            _logger.LogDebug("Updating flow entity. Id: {Id}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated flow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (Core.Exceptions.ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed updating flow entity. Id: {Id}, Entity: {Entity}, Property: {Property}, Value: {Value}, ReferencedEntity: {ReferencedEntity}, User: {User}, RequestId: {RequestId}",
                id, ex.EntityType, ex.ForeignKeyProperty, ex.ForeignKeyValue, ex.ReferencedEntityType, userContext, HttpContext.TraceIdentifier);
            return BadRequest(new
            {
                message = ex.Message,
                apiMessage = ex.GetApiErrorMessage(),
                entity = ex.EntityType,
                property = ex.ForeignKeyProperty,
                value = ex.ForeignKeyValue,
                referencedEntity = ex.ReferencedEntityType
            });
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented update of flow entity. Id: {Id}, Error: {Error}, References: {OrchestratedFlowCount} orchestrated flows, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    orchestratedFlowEntityCount = ex.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0,
                    totalReferences = ex.FlowEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.FlowEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating flow entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Flow entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Flow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flow entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the flow");
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Delete flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Flow entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with ID {id} not found");
            }

            _logger.LogDebug("Deleting flow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete flow entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the flow entity");
            }

            _logger.LogInformation("Successfully deleted flow entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of flow entity. Id: {Id}, Error: {Error}, References: {OrchestratedFlowCount} orchestrated flows, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    orchestratedFlowEntityCount = ex.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0,
                    totalReferences = ex.FlowEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.FlowEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flow entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the flow");
        }
    }
}
