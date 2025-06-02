namespace EntitiesManager.Core.Interfaces.Entities;

/// <summary>
/// Interface for entities that require composite key validation and uniqueness enforcement
/// </summary>
public interface ICompositeKeyEntity
{
    /// <summary>
    /// Gets the composite key for uniqueness validation
    /// </summary>
    /// <returns>Composite key string used for uniqueness checks</returns>
    string GetCompositeKey();
}
