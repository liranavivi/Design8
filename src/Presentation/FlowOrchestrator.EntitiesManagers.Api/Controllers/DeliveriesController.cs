using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryEntityRepository _repository;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(
        IDeliveryEntityRepository repository,
        ILogger<DeliveriesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetAll deliveries request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();
            var count = entities.Count();

            _logger.LogInformation("Successfully retrieved {Count} delivery entities. User: {User}, RequestId: {RequestId}",
                count, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all delivery entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving delivery entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged deliveries request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged delivery entities. Page: {Page}/{TotalPages}, PageSize: {PageSize}, TotalCount: {TotalCount}, User: {User}, RequestId: {RequestId}",
                page, totalPages, pageSize, totalCount, userContext, HttpContext.TraceIdentifier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged delivery entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                originalPage, originalPageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving paged delivery entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetById delivery request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Delivery entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Delivery entity with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, entity.SchemaId, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the delivery entity");
        }
    }

    [HttpGet("composite/{version}/{name}")]
    public async Task<ActionResult<DeliveryEntity>> GetByCompositeKey(string version, string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        // DeliveryEntity composite key format: "version_name"
        var compositeKey = $"{version}_{name}";

        _logger.LogInformation("Starting GetByCompositeKey delivery request. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
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
                _logger.LogWarning("Delivery entity not found by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Delivery entity with version '{version}' and name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved delivery entity by composite key. CompositeKey: {CompositeKey}, Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
                compositeKey, entity.Id, entity.Version, entity.Name, entity.SchemaId, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery entity by composite key. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                version, name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the delivery entity");
        }
    }



    [HttpGet("version/{version}")]
    public async Task<ActionResult<IEnumerable<DeliveryEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetByVersion delivery request. Version: {Version}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved {Count} delivery entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                count, version, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving delivery entities");
        }
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<IEnumerable<DeliveryEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting GetByName delivery request. Name: {Name}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved {Count} delivery entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                count, name, userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving delivery entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryEntity>> Create([FromBody] DeliveryEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create delivery request. CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            compositeKey, userContext, HttpContext.TraceIdentifier);

        if (entity == null)
        {
            _logger.LogWarning("Null entity provided for delivery creation. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest("Delivery entity cannot be null");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Invalid model state for delivery creation. Errors: {Errors}, User: {User}, RequestId: {RequestId}",
                errorMessage, userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating delivery entity with details. Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Version, entity.Name, entity.SchemaId, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new DeliveryEntity. Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                    entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Version, created.Name, created.SchemaId, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during delivery creation. SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
                entity?.SchemaId, userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning(ex, "Duplicate key conflict creating delivery entity. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery entity. Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the delivery entity");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DeliveryEntity>> Update(Guid id, [FromBody] DeliveryEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update delivery request. Id: {Id}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (entity == null)
        {
            _logger.LogWarning("Null entity provided for delivery update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("Delivery entity cannot be null");
        }

        if (id != entity.Id)
        {
            _logger.LogWarning("ID mismatch in delivery update request. URL ID: {UrlId}, Entity ID: {EntityId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match entity ID");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Invalid model state for delivery update. Id: {Id}, Errors: {Errors}, User: {User}, RequestId: {RequestId}",
                id, errorMessage, userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Delivery entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Delivery entity with ID {id} not found");
            }

            entity.UpdatedBy = userContext;
            entity.CreatedAt = existingEntity.CreatedAt;
            entity.CreatedBy = existingEntity.CreatedBy;

            _logger.LogDebug("Updating delivery entity with details. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, UpdatedBy: {UpdatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, entity.SchemaId, entity.UpdatedBy, userContext, HttpContext.TraceIdentifier);

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Version, updated.Name, updated.SchemaId, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during delivery update. Id: {Id}, SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
                id, entity?.SchemaId, userContext, HttpContext.TraceIdentifier);
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
            _logger.LogWarning("Referential integrity violation prevented update of delivery entity. Id: {Id}, Error: {Error}, References: {AssignmentCount} assignments, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.DeliveryEntityReferences?.AssignmentEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            var detailedMessage = ex.GetDetailedMessage();
            return Conflict(new
            {
                error = detailedMessage,
                errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                referencingEntities = new
                {
                    assignmentEntityCount = ex.DeliveryEntityReferences?.AssignmentEntityCount ?? 0,
                    totalReferences = ex.DeliveryEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.DeliveryEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the delivery entity");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Starting Delete delivery request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Delivery entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Delivery entity with ID {id} not found");
            }

            _logger.LogDebug("Deleting delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
                id, existingEntity.Version, existingEntity.Name, existingEntity.SchemaId, userContext, HttpContext.TraceIdentifier);

            var success = await _repository.DeleteAsync(id);

            if (!success)
            {
                _logger.LogError("Failed to delete delivery entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete delivery entity");
            }

            _logger.LogInformation("Successfully deleted delivery entity. Id: {Id}, Version: {Version}, Name: {Name}, SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
                id, existingEntity.Version, existingEntity.Name, existingEntity.SchemaId, userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of delivery entity. Id: {Id}, Error: {Error}, References: {AssignmentCount} assignments, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.DeliveryEntityReferences?.AssignmentEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            var detailedMessage = ex.GetDetailedMessage();
            return Conflict(new
            {
                error = detailedMessage,
                errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                referencingEntities = new
                {
                    assignmentEntityCount = ex.DeliveryEntityReferences?.AssignmentEntityCount ?? 0,
                    totalReferences = ex.DeliveryEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.DeliveryEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the delivery entity");
        }
    }

    // ========================================
    // FALLBACK ROUTES FOR EMPTY PARAMETERS
    // ========================================
    // These routes handle cases where empty parameters are provided
    // and return proper 400 Bad Request responses instead of 404 Not Found

    [HttpGet("version")]
    public ActionResult<IEnumerable<DeliveryEntity>> GetByVersionEmpty()
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

    [HttpGet("name")]
    public ActionResult<IEnumerable<DeliveryEntity>> GetByNameEmpty()
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

    [HttpGet("composite/{version}/")]
    public ActionResult<DeliveryEntity> GetByCompositeKeyEmptyName(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty name parameter in GetByCompositeKey request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        return BadRequest(new {
            error = "Invalid name parameter",
            message = "Name parameter cannot be null or empty",
            parameter = "name",
            providedVersion = version
        });
    }

    [HttpGet("composite")]
    public ActionResult<DeliveryEntity> GetByCompositeKeyEmpty()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogWarning("Empty composite key parameters in GetByCompositeKey request (no parameters provided). User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        return BadRequest(new {
            error = "Invalid composite key parameters",
            message = "Both version and name parameters are required",
            parameters = new[] { "version", "name" }
        });
    }
}
