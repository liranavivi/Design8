using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessorsController : ControllerBase
{
    private readonly IProcessorEntityRepository _repository;
    private readonly ILogger<ProcessorsController> _logger;

    public ProcessorsController(
        IProcessorEntityRepository repository,
        ILogger<ProcessorsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcessorEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll processors request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all processor entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all processor entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving processor entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetPaged processors request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged processor entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged processor entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving processor entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProcessorEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById processor request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Processor entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Processor with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved processor entity by ID. Id: {Id}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processor entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the processor entity");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<ProcessorEntity> GetByIdFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in GetById processor request. Id: {Id}, User: {User}, RequestId: {RequestId}",
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

    [HttpGet("by-key/{version}/{name}")]
    public async Task<ActionResult<ProcessorEntity>> GetByCompositeKey(string version, string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        // ProcessorEntity composite key format: "version_name" (URL decode to handle spaces and special characters)
        var decodedVersion = WebUtility.UrlDecode(version);
        var decodedName = WebUtility.UrlDecode(name);
        var compositeKey = $"{decodedVersion}_{decodedName}";

        _logger.LogInformation("Starting GetByCompositeKey processor request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            version, name, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("Processor entity not found by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Processor with version '{version}' and name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved processor entity by composite key. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processor entity by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the processor entity");
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<ProcessorEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName processor request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved processor entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processor entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving processor entities");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<ProcessorEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByVersion processor request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved processor entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processor entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving processor entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProcessorEntity>> Create([FromBody] ProcessorEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create processor request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create processor request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating processor entity with details. Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new ProcessorEntity. Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created processor entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during processor creation. InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}, User: {User}, RequestId: {RequestId}",
                entity?.InputSchemaId, entity?.OutputSchemaId, userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning(ex, "Duplicate key conflict creating processor entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating processor entity. Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the processor");
        }
    }

    [HttpPut("{id}")]
    public ActionResult<ProcessorEntity> UpdateFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Update processor request. Id: {Id}, User: {User}, RequestId: {RequestId}",
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
    public async Task<ActionResult<ProcessorEntity>> Update(Guid id, [FromBody] ProcessorEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update processor request. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update processor request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update processor request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Processor entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Processor with ID {id} not found");
            }

            _logger.LogDebug("Updating processor entity. Id: {Id}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated processor entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during processor update. Id: {Id}, InputSchemaId: {InputSchemaId}, OutputSchemaId: {OutputSchemaId}, User: {User}, RequestId: {RequestId}",
                id, entity?.InputSchemaId, entity?.OutputSchemaId, userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning("Referential integrity violation prevented update of processor entity. Id: {Id}, Error: {Error}, References: {StepCount} steps, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.ProcessorEntityReferences?.StepEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    stepEntityCount = ex.ProcessorEntityReferences?.StepEntityCount ?? 0,
                    totalReferences = ex.ProcessorEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.ProcessorEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating processor entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Processor entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Processor with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processor entity. Id: {Id}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the processor");
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteFallback(string id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Invalid GUID format in Delete processor request. Id: {Id}, User: {User}, RequestId: {RequestId}",
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

        _logger.LogInformation("Starting Delete processor request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Processor entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Processor with ID {id} not found");
            }

            _logger.LogDebug("Deleting processor entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete processor entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the processor entity");
            }

            _logger.LogInformation("Successfully deleted processor entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of processor entity. Id: {Id}, Error: {Error}, References: {StepCount} steps, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.ProcessorEntityReferences?.StepEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    stepEntityCount = ex.ProcessorEntityReferences?.StepEntityCount ?? 0,
                    totalReferences = ex.ProcessorEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.ProcessorEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting processor entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the processor");
        }
    }
}
