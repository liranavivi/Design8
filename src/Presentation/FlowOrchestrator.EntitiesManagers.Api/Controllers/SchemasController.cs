using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchemasController : ControllerBase
{
    private readonly ISchemaEntityRepository _repository;
    private readonly ILogger<SchemasController> _logger;

    public SchemasController(
        ISchemaEntityRepository repository,
        ILogger<SchemasController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchemaEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetAll schemas request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();
            var count = entities.Count();

            _logger.LogInformation("Successfully retrieved {Count} schema entities. User: {User}, RequestId: {RequestId}",
                count, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all schema entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving schema entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged schemas request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
            page, pageSize, userContext, HttpContext.TraceIdentifier);

        try
        {
            // Strict validation - return 400 for invalid parameters instead of auto-correcting
            if (page < 1)
            {
                _logger.LogWarning("Invalid page parameter {Page} provided. User: {User}, RequestId: {RequestId}",
                    originalPage, userContext, HttpContext.TraceIdentifier);
                return BadRequest(new
                {
                    error = "Invalid page parameter",
                    message = "Page must be greater than 0",
                    parameter = "page",
                    value = originalPage
                });
            }

            if (pageSize < 1)
            {
                _logger.LogWarning("Invalid pageSize parameter {PageSize} provided. User: {User}, RequestId: {RequestId}",
                    originalPageSize, userContext, HttpContext.TraceIdentifier);
                return BadRequest(new
                {
                    error = "Invalid pageSize parameter",
                    message = "PageSize must be greater than 0",
                    parameter = "pageSize",
                    value = originalPageSize
                });
            }
            else if (pageSize > 100)
            {
                _logger.LogWarning("PageSize parameter {PageSize} exceeds maximum. User: {User}, RequestId: {RequestId}",
                    originalPageSize, userContext, HttpContext.TraceIdentifier);
                return BadRequest(new
                {
                    error = "Invalid pageSize parameter",
                    message = "PageSize cannot exceed 100",
                    parameter = "pageSize",
                    value = originalPageSize,
                    maximum = 100
                });
            }

            var entities = await _repository.GetPagedAsync(page, pageSize);
            var totalCount = await _repository.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var result = new
            {
                data = entities,
                page = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = totalPages
            };

            _logger.LogInformation("Successfully retrieved paged schema entities. Page: {Page}/{TotalPages}, PageSize: {PageSize}, TotalCount: {TotalCount}, User: {User}, RequestId: {RequestId}",
                page, totalPages, pageSize, totalCount, userContext, HttpContext.TraceIdentifier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged schema entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                originalPage, originalPageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving paged schema entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchemaEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetById schema request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Schema entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Schema entity with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved schema entity. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, entity.Definition, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the schema entity");
        }
    }

    [HttpGet("composite/{version}/{name}")]
    public async Task<ActionResult<SchemaEntity>> GetByCompositeKey(string version, string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        // SchemaEntity composite key format: "version_name"
        var compositeKey = $"{version}_{name}";

        _logger.LogInformation("Starting GetByCompositeKey schema request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            version, name, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Empty or null version/name provided. Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                    version, name, userContext, HttpContext.TraceIdentifier);
                return BadRequest("Version and name cannot be empty");
            }

            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("Schema entity not found by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Schema entity with version '{version}' and name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved schema entity by composite key. CompositeKey: {CompositeKey}, Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, User: {User}, RequestId: {RequestId}",
                compositeKey, entity.Id, entity.Version, entity.Name, entity.Definition, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema entity by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the schema entity");
        }
    }

    [HttpGet("definition/{definition}")]
    public async Task<ActionResult<IEnumerable<SchemaEntity>>> GetByDefinition(string definition)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetByDefinition schema request. Definition: {Definition}, User: {User}, RequestId: {RequestId}",
            definition, userContext, HttpContext.TraceIdentifier);

        try
        {
            if (string.IsNullOrWhiteSpace(definition))
            {
                _logger.LogWarning("Empty or null definition provided. User: {User}, RequestId: {RequestId}",
                    userContext, HttpContext.TraceIdentifier);
                return BadRequest("Definition cannot be empty");
            }

            var entities = await _repository.GetByDefinitionAsync(definition);
            var count = entities.Count();

            _logger.LogInformation("Successfully retrieved {Count} schema entities by definition. Definition: {Definition}, User: {User}, RequestId: {RequestId}",
                count, definition, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema entities by definition. Definition: {Definition}, User: {User}, RequestId: {RequestId}",
                definition, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving schema entities");
        }
    }

    [HttpGet("version/{version}")]
    public async Task<ActionResult<IEnumerable<SchemaEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetByVersion schema request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                _logger.LogWarning("Empty or null version provided. User: {User}, RequestId: {RequestId}",
                    userContext, HttpContext.TraceIdentifier);
                return BadRequest("Version cannot be empty");
            }

            var entities = await _repository.GetByVersionAsync(version);
            var count = entities.Count();

            _logger.LogInformation("Successfully retrieved {Count} schema entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                count, version, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving schema entities");
        }
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<IEnumerable<SchemaEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetByName schema request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Empty or null name provided. User: {User}, RequestId: {RequestId}",
                    userContext, HttpContext.TraceIdentifier);
                return BadRequest("Name cannot be empty");
            }

            var entities = await _repository.GetByNameAsync(name);
            var count = entities.Count();

            _logger.LogInformation("Successfully retrieved {Count} schema entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                count, name, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving schema entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<SchemaEntity>> Create([FromBody] SchemaEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create schema request. CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            compositeKey, userContext, HttpContext.TraceIdentifier);

        if (entity == null)
        {
            _logger.LogWarning("Null entity provided for schema creation. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest("Schema entity cannot be null");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Invalid model state for schema creation. Errors: {Errors}, User: {User}, RequestId: {RequestId}",
                errorMessage, userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating schema entity with details. Version: {Version}, Name: {Name}, Definition: {Definition}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.Definition, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new SchemaEntity. Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                    entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created schema entity. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.Definition, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating schema entity. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schema entity. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the schema entity");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SchemaEntity>> Update(Guid id, [FromBody] SchemaEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update schema request. Id: {Id}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (entity == null)
        {
            _logger.LogWarning("Null entity provided for schema update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("Schema entity cannot be null");
        }

        if (id != entity.Id)
        {
            _logger.LogWarning("ID mismatch in schema update request. URL ID: {UrlId}, Entity ID: {EntityId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match entity ID");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Invalid model state for schema update. Id: {Id}, Errors: {Errors}, User: {User}, RequestId: {RequestId}",
                id, errorMessage, userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Schema entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Schema entity with ID {id} not found");
            }

            entity.UpdatedBy = userContext;
            entity.CreatedAt = existingEntity.CreatedAt;
            entity.CreatedBy = existingEntity.CreatedBy;

            _logger.LogDebug("Updating schema entity with details. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, UpdatedBy: {UpdatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, entity.Definition, entity.UpdatedBy, userContext, HttpContext.TraceIdentifier);

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated schema entity. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.Definition, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented update of schema entity. Id: {Id}, Error: {Error}, References: {TotalReferences} total ({AssignmentCount} assignments, {AddressCount} addresses, {DeliveryCount} deliveries, {ProcessorInputCount} processor inputs, {ProcessorOutputCount} processor outputs), User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.SchemaEntityReferences?.TotalReferences ?? 0, ex.SchemaEntityReferences?.AssignmentEntityCount ?? 0, ex.SchemaEntityReferences?.AddressEntityCount ?? 0, ex.SchemaEntityReferences?.DeliveryEntityCount ?? 0, ex.SchemaEntityReferences?.ProcessorEntityInputCount ?? 0, ex.SchemaEntityReferences?.ProcessorEntityOutputCount ?? 0, userContext, HttpContext.TraceIdentifier);

            var detailedMessage = ex.GetDetailedMessage();
            return Conflict(new
            {
                error = detailedMessage,
                errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                referencingEntities = new
                {
                    assignmentEntityCount = ex.SchemaEntityReferences?.AssignmentEntityCount ?? 0,
                    addressEntityCount = ex.SchemaEntityReferences?.AddressEntityCount ?? 0,
                    deliveryEntityCount = ex.SchemaEntityReferences?.DeliveryEntityCount ?? 0,
                    processorEntityInputCount = ex.SchemaEntityReferences?.ProcessorEntityInputCount ?? 0,
                    processorEntityOutputCount = ex.SchemaEntityReferences?.ProcessorEntityOutputCount ?? 0,
                    totalReferences = ex.SchemaEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.SchemaEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating schema entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schema entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the schema entity");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting Delete schema request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Schema entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Schema entity with ID {id} not found");
            }

            _logger.LogDebug("Deleting schema entity. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, User: {User}, RequestId: {RequestId}",
                id, existingEntity.Version, existingEntity.Name, existingEntity.Definition, userContext, HttpContext.TraceIdentifier);

            var success = await _repository.DeleteAsync(id);

            if (!success)
            {
                _logger.LogError("Failed to delete schema entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete schema entity");
            }

            _logger.LogInformation("Successfully deleted schema entity. Id: {Id}, Version: {Version}, Name: {Name}, Definition: {Definition}, User: {User}, RequestId: {RequestId}",
                id, existingEntity.Version, existingEntity.Name, existingEntity.Definition, userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of schema entity. Id: {Id}, Error: {Error}, References: {TotalReferences} total ({AssignmentCount} assignments, {AddressCount} addresses, {DeliveryCount} deliveries, {ProcessorInputCount} processor inputs, {ProcessorOutputCount} processor outputs), User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.SchemaEntityReferences?.TotalReferences ?? 0, ex.SchemaEntityReferences?.AssignmentEntityCount ?? 0, ex.SchemaEntityReferences?.AddressEntityCount ?? 0, ex.SchemaEntityReferences?.DeliveryEntityCount ?? 0, ex.SchemaEntityReferences?.ProcessorEntityInputCount ?? 0, ex.SchemaEntityReferences?.ProcessorEntityOutputCount ?? 0, userContext, HttpContext.TraceIdentifier);

            var detailedMessage = ex.GetDetailedMessage();
            return Conflict(new
            {
                error = detailedMessage,
                errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                referencingEntities = new
                {
                    assignmentEntityCount = ex.SchemaEntityReferences?.AssignmentEntityCount ?? 0,
                    addressEntityCount = ex.SchemaEntityReferences?.AddressEntityCount ?? 0,
                    deliveryEntityCount = ex.SchemaEntityReferences?.DeliveryEntityCount ?? 0,
                    processorEntityInputCount = ex.SchemaEntityReferences?.ProcessorEntityInputCount ?? 0,
                    processorEntityOutputCount = ex.SchemaEntityReferences?.ProcessorEntityOutputCount ?? 0,
                    totalReferences = ex.SchemaEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.SchemaEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schema entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the schema entity");
        }
    }
}
