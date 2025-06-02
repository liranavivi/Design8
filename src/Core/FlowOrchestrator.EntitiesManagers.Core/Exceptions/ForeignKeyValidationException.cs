namespace FlowOrchestrator.EntitiesManagers.Core.Exceptions;

/// <summary>
/// Exception thrown when foreign key validation fails during CREATE or UPDATE operations.
/// This occurs when a referenced entity (e.g., ProtocolEntity) does not exist.
/// </summary>
public class ForeignKeyValidationException : Exception
{
    public string EntityType { get; }
    public string ForeignKeyProperty { get; }
    public object ForeignKeyValue { get; }
    public string ReferencedEntityType { get; }

    public ForeignKeyValidationException(
        string entityType, 
        string foreignKeyProperty, 
        object foreignKeyValue, 
        string referencedEntityType)
        : base($"Foreign key validation failed for {entityType}.{foreignKeyProperty}. Referenced {referencedEntityType} with ID '{foreignKeyValue}' does not exist.")
    {
        EntityType = entityType;
        ForeignKeyProperty = foreignKeyProperty;
        ForeignKeyValue = foreignKeyValue;
        ReferencedEntityType = referencedEntityType;
    }

    public ForeignKeyValidationException(
        string entityType, 
        string foreignKeyProperty, 
        object foreignKeyValue, 
        string referencedEntityType, 
        Exception innerException)
        : base($"Foreign key validation failed for {entityType}.{foreignKeyProperty}. Referenced {referencedEntityType} with ID '{foreignKeyValue}' does not exist.", innerException)
    {
        EntityType = entityType;
        ForeignKeyProperty = foreignKeyProperty;
        ForeignKeyValue = foreignKeyValue;
        ReferencedEntityType = referencedEntityType;
    }

    /// <summary>
    /// Gets a detailed error message suitable for API responses
    /// </summary>
    public string GetApiErrorMessage()
    {
        return $"The referenced {ReferencedEntityType} with ID '{ForeignKeyValue}' does not exist. Please provide a valid {ForeignKeyProperty}.";
    }
}
