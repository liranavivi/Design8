using FlowOrchestrator.EntitiesManagers.Core.Entities;
using FlowOrchestrator.EntitiesManagers.Core.Exceptions;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowOrchestrator.EntitiesManagers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressesController : ControllerBase
{
    private readonly IAddressEntityRepository _repository;
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(
        IAddressEntityRepository repository,
        ILogger<AddressesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AddressEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll addresses request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all address entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all address entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving address entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged addresses request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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
            var entities = await _repository.GetAllAsync();
            var totalCount = entities.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedEntities = entities
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new
            {
                data = pagedEntities,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            };

            _logger.LogInformation("Successfully retrieved paged address entities. Page: {Page}, PageSize: {PageSize}, TotalCount: {TotalCount}, User: {User}, RequestId: {RequestId}",
                page, pageSize, totalCount, userContext, HttpContext.TraceIdentifier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged address entities. OriginalPage: {OriginalPage}, OriginalPageSize: {OriginalPageSize}, User: {User}, RequestId: {RequestId}",
                originalPage, originalPageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving paged address entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AddressEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById address request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Address entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Address with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved address entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the address entity");
        }
    }

    [HttpGet("by-address/{address}")]
    public async Task<ActionResult<IEnumerable<AddressEntity>>> GetByAddress(string address)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        // Validate address parameter
        if (string.IsNullOrEmpty(address))
        {
            _logger.LogWarning("Empty address parameter in GetByAddress request. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid address parameter",
                message = "Address parameter cannot be null or empty",
                parameter = "address"
            });
        }

        _logger.LogInformation("Starting GetByAddress request. Address: {Address}, User: {User}, RequestId: {RequestId}",
            address, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByAddressAsync(address);

            _logger.LogInformation("Successfully retrieved address entities by address. Address: {Address}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                address, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address entities by address. Address: {Address}, User: {User}, RequestId: {RequestId}",
                address, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving address entities by address");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<AddressEntity>>> GetByVersion(string version)
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

        _logger.LogInformation("Starting GetByVersion request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved address entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving address entities by version");
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<AddressEntity>>> GetByName(string name)
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

        _logger.LogInformation("Starting GetByName request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved address entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving address entities by name");
        }
    }

    [HttpGet("by-key/{address}/{version}")]
    public async Task<ActionResult<AddressEntity>> GetByCompositeKey(string address, string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        // Validate parameters
        if (string.IsNullOrEmpty(address))
        {
            _logger.LogWarning("Empty address parameter in GetByCompositeKey request. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid address parameter",
                message = "Address parameter cannot be null or empty",
                parameter = "address"
            });
        }

        if (string.IsNullOrEmpty(version))
        {
            _logger.LogWarning("Empty version parameter in GetByCompositeKey request. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest(new {
                error = "Invalid version parameter",
                message = "Version parameter cannot be null or empty",
                parameter = "version"
            });
        }

        // AddressEntity composite key format: "version_name_address"
        // We need to get the name from the address first, so let's try to find by address and version

        _logger.LogInformation("Starting GetByCompositeKey address request. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
            address, version, userContext, HttpContext.TraceIdentifier);

        try
        {
            // First get by address to find the entity, then check version
            var entities = await _repository.GetByAddressAsync(address);
            var entity = entities.FirstOrDefault(e => e.Version == version);

            if (entity == null)
            {
                _logger.LogWarning("Address entity not found by address and version. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
                    address, version, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Address with address '{address}' and version '{version}' not found");
            }

            _logger.LogInformation("Successfully retrieved address entity by composite key. Id: {Id}, Version: {Version}, Name: {Name}, Address: {Address}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Version, entity.Name, entity.Address, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address entity by composite key. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
                address, version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the address entity");
        }
    }

    [HttpPost]
    public async Task<ActionResult<AddressEntity>> Create([FromBody] AddressEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        if (entity == null)
        {
            _logger.LogWarning("Create address request with null entity. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest("Address entity cannot be null");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Create address request with invalid model state. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty; // Ensure MongoDB generates the ID

            _logger.LogDebug("Creating address entity with details. Address: {Address}, Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Address, entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new AddressEntity. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Address, entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created address entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Address, created.Version, created.Name, userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during address creation. SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogWarning(ex, "Duplicate composite key during address creation. Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, entity?.Name, userContext, HttpContext.TraceIdentifier);
            return Conflict(new {
                error = ex.Message,
                errorCode = "DUPLICATE_COMPOSITE_KEY",
                compositeKey = $"{entity?.Version}_{entity?.Name}_{entity?.Address}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address entity. Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, entity?.Name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the address entity");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AddressEntity>> Update(Guid id, [FromBody] AddressEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        if (entity == null)
        {
            _logger.LogWarning("Update address request with null entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("Address entity cannot be null");
        }

        if (id != entity.Id)
        {
            _logger.LogWarning("Update address request with mismatched IDs. RouteId: {RouteId}, EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID mismatch between route and entity");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Update address request with invalid model state. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Update address request for non-existent entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Address with ID {id} not found");
            }

            entity.UpdatedBy = userContext;

            _logger.LogDebug("Updating address entity with details. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, UpdatedBy: {UpdatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Id, entity.Address, entity.Version, entity.Name, entity.UpdatedBy, userContext, HttpContext.TraceIdentifier);

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated address entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Address, updated.Version, updated.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ForeignKeyValidationException ex)
        {
            _logger.LogWarning(ex, "Foreign key validation failed during address update. Id: {Id}, SchemaId: {SchemaId}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogWarning(ex, "Referential integrity violation during address update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the address entity");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete address request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("Delete address request for non-existent entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Address with ID {id} not found");
            }

            _logger.LogDebug("Deleting address entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, existingEntity.Address, existingEntity.Version, existingEntity.Name, userContext, HttpContext.TraceIdentifier);

            var success = await _repository.DeleteAsync(id);

            if (success)
            {
                _logger.LogInformation("Successfully deleted address entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NoContent();
            }
            else
            {
                _logger.LogWarning("Failed to delete address entity (not found). Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Address with ID {id} not found");
            }
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning(ex, "Referential integrity violation during address deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the address entity");
        }
    }
}
