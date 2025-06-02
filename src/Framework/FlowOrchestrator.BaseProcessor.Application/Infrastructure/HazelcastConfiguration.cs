using FlowOrchestrator.BaseProcessor.Application.Models;
using FlowOrchestrator.BaseProcessor.Application.Services;
using Hazelcast;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowOrchestrator.BaseProcessor.Application.Infrastructure;

/// <summary>
/// Extension methods for configuring Hazelcast client
/// Following EntitiesManager.Api patterns for consistency
/// </summary>
public static class HazelcastConfiguration
{
    /// <summary>
    /// Adds Hazelcast client and cache services
    /// Following EntitiesManager.Api configuration patterns
    /// </summary>
    public static IServiceCollection AddHazelcastClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var hazelcastConfig = configuration.GetSection("Hazelcast").Get<ProcessorHazelcastConfiguration>()
            ?? new ProcessorHazelcastConfiguration();

        // Configure Hazelcast client following EntitiesManager.Api patterns
        // Use lazy initialization to avoid blocking during service registration
        services.AddSingleton<Lazy<Task<IHazelcastClient>>>(serviceProvider =>
        {
            return new Lazy<Task<IHazelcastClient>>(async () =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<HazelcastCacheService>>();

                var options = new HazelcastOptionsBuilder()
                    .With(o =>
                    {
                        o.ClusterName = hazelcastConfig.ClusterName;
                        foreach (var address in hazelcastConfig.NetworkConfig.Addresses)
                        {
                            o.Networking.Addresses.Add(address);
                        }
                    })
                    .Build();

                logger.LogInformation("Connecting to Hazelcast cluster: {ClusterName}", hazelcastConfig.ClusterName);
                return await HazelcastClientFactory.StartNewClientAsync(options);
            });
        });

        // Register cache services
        services.AddSingleton<ICacheService, HazelcastCacheService>();

        return services;
    }
}

/// <summary>
/// Hazelcast-specific cache service implementation
/// </summary>
public class HazelcastCacheService : ICacheService
{
    private readonly Lazy<Task<IHazelcastClient>> _hazelcastClientFactory;
    private readonly ILogger<HazelcastCacheService> _logger;
    private readonly System.Diagnostics.ActivitySource _activitySource;

    public HazelcastCacheService(
        Lazy<Task<IHazelcastClient>> hazelcastClientFactory,
        ILogger<HazelcastCacheService> logger)
    {
        _hazelcastClientFactory = hazelcastClientFactory;
        _logger = logger;
        _activitySource = new System.Diagnostics.ActivitySource("BaseProcessorApplication.Cache");
    }

    private async Task<IHazelcastClient> GetClientAsync()
    {
        return await _hazelcastClientFactory.Value;
    }

    public async Task<string?> GetAsync(string mapName, string key)
    {
        using var activity = _activitySource.StartActivity("Cache.Get");
        activity?.SetTag("cache.operation", "get")
                ?.SetTag("cache.map_name", mapName)
                ?.SetTag("cache.key", key);

        try
        {
            var client = await GetClientAsync();
            var map = await client.GetMapAsync<string, string>(mapName);
            var result = await map.GetAsync(key);

            activity?.SetTag("cache.hit", result != null);

            _logger.LogDebug("Retrieved data from cache. MapName: {MapName}, Key: {Key}, Found: {Found}",
                mapName, key, result != null);

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to retrieve data from cache. MapName: {MapName}, Key: {Key}",
                mapName, key);
            throw;
        }
    }

    public async Task SetAsync(string mapName, string key, string value)
    {
        using var activity = _activitySource.StartActivity("Cache.Set");
        activity?.SetTag("cache.operation", "set")
                ?.SetTag("cache.map_name", mapName)
                ?.SetTag("cache.key", key);

        try
        {
            var client = await GetClientAsync();
            var map = await client.GetMapAsync<string, string>(mapName);
            await map.SetAsync(key, value);

            _logger.LogDebug("Saved data to cache. MapName: {MapName}, Key: {Key}",
                mapName, key);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to save data to cache. MapName: {MapName}, Key: {Key}",
                mapName, key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string mapName, string key)
    {
        using var activity = _activitySource.StartActivity("Cache.Exists");
        activity?.SetTag("cache.operation", "exists")
                ?.SetTag("cache.map_name", mapName)
                ?.SetTag("cache.key", key);

        try
        {
            var client = await GetClientAsync();
            var map = await client.GetMapAsync<string, string>(mapName);
            var exists = await map.ContainsKeyAsync(key);

            activity?.SetTag("cache.hit", exists);

            _logger.LogDebug("Checked cache key existence. MapName: {MapName}, Key: {Key}, Exists: {Exists}",
                mapName, key, exists);

            return exists;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to check cache key existence. MapName: {MapName}, Key: {Key}",
                mapName, key);
            throw;
        }
    }

    public async Task RemoveAsync(string mapName, string key)
    {
        using var activity = _activitySource.StartActivity("Cache.Remove");
        activity?.SetTag("cache.operation", "remove")
                ?.SetTag("cache.map_name", mapName)
                ?.SetTag("cache.key", key);

        try
        {
            var client = await GetClientAsync();
            var map = await client.GetMapAsync<string, string>(mapName);
            await map.RemoveAsync(key);

            _logger.LogDebug("Removed data from cache. MapName: {MapName}, Key: {Key}",
                mapName, key);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to remove data from cache. MapName: {MapName}, Key: {Key}",
                mapName, key);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Simple health check by trying to get a map
            var client = await GetClientAsync();
            var testMap = await client.GetMapAsync<string, string>("health-check");
            return testMap != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hazelcast health check failed");
            return false;
        }
    }

    public void Dispose()
    {
        _activitySource?.Dispose();
    }
}
