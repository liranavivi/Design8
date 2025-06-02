namespace FlowOrchestrator.BaseProcessor.Application.Services;

/// <summary>
/// Interface for cache service operations
/// </summary>
public interface ICacheService : IDisposable
{
    /// <summary>
    /// Retrieves data from cache
    /// </summary>
    /// <param name="mapName">Name of the cache map</param>
    /// <param name="key">Cache key</param>
    /// <returns>Cached data or null if not found</returns>
    Task<string?> GetAsync(string mapName, string key);

    /// <summary>
    /// Stores data in cache
    /// </summary>
    /// <param name="mapName">Name of the cache map</param>
    /// <param name="key">Cache key</param>
    /// <param name="value">Data to store</param>
    /// <returns>Task representing the operation</returns>
    Task SetAsync(string mapName, string key, string value);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="mapName">Name of the cache map</param>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string mapName, string key);

    /// <summary>
    /// Removes data from cache
    /// </summary>
    /// <param name="mapName">Name of the cache map</param>
    /// <param name="key">Cache key</param>
    /// <returns>Task representing the operation</returns>
    Task RemoveAsync(string mapName, string key);

    /// <summary>
    /// Checks if the cache service is healthy and accessible
    /// </summary>
    /// <returns>True if healthy, false otherwise</returns>
    Task<bool> IsHealthyAsync();
}
