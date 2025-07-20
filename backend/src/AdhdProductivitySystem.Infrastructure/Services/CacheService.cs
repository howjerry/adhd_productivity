using AdhdProductivitySystem.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace AdhdProductivitySystem.Infrastructure.Services;

/// <summary>
/// Redis 快取服務實作，支援標籤式快取失效和 Cache Aside 模式
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IDatabase? _database;
    private readonly ILogger<CacheService> _logger;
    private readonly DistributedCacheEntryOptions _defaultOptions;
    
    private const string TagPrefix = "tag:";
    private const string TaggedKeyPrefix = "tagged_keys:";

    public CacheService(
        IDistributedCache distributedCache, 
        IConnectionMultiplexer? redis,
        ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _database = redis?.GetDatabase();
        _logger = logger;
        
        _defaultOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (cached == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = expiry.HasValue 
                ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }
                : _defaultOptions;

            var serializedValue = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);

            // 如果有標籤且 Redis 可用，建立標籤關聯
            if (tags != null && tags.Length > 0 && _database != null)
            {
                await SetTagAssociationsAsync(key, tags, expiry ?? _defaultOptions.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(15));
            }

            _logger.LogDebug("Cached value for key: {Key} with tags: {Tags}", key, tags != null ? string.Join(", ", tags) : "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            
            // 同時移除標籤關聯
            if (_database != null)
            {
                await RemoveTagAssociationsAsync(key);
            }
            
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (_redis == null)
        {
            _logger.LogWarning("Redis connection not available for pattern removal: {Pattern}", pattern);
            return;
        }

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);

            foreach (var key in keys)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
                await RemoveTagAssociationsAsync(key);
            }

            _logger.LogDebug("Removed cache for pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for pattern: {Pattern}", pattern);
        }
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await InvalidateByTagsAsync(new[] { tag }, cancellationToken);
    }

    public async Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        if (_database == null)
        {
            _logger.LogWarning("Redis connection not available for tag invalidation");
            return;
        }

        try
        {
            foreach (var tag in tags)
            {
                var taggedKeysKey = $"{TaggedKeyPrefix}{tag}";
                var taggedKeys = await _database.SetMembersAsync(taggedKeysKey);

                foreach (var key in taggedKeys)
                {
                    await _distributedCache.RemoveAsync(key, cancellationToken);
                    await RemoveTagAssociationsAsync(key);
                }

                // 移除標籤集合本身
                await _database.KeyDeleteAsync(taggedKeysKey);
                
                _logger.LogDebug("Invalidated cache for tag: {Tag}, affected keys: {KeyCount}", tag, taggedKeys.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
    {
        // 先嘗試從快取取得
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // 快取未命中，執行工廠方法
        try
        {
            var value = await factory();
            
            // 將結果存入快取
            await SetAsync(key, value, expiry, tags, cancellationToken);
            
            _logger.LogDebug("Cache miss resolved for key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync factory for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await _distributedCache.GetAsync(key, cancellationToken);
            return cached != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (_database == null)
        {
            _logger.LogWarning("Redis connection not available for setting expiry");
            return;
        }

        try
        {
            await _database.KeyExpireAsync(key, expiry);
            _logger.LogDebug("Set expiry for key: {Key} to {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiry for key: {Key}", key);
        }
    }

    /// <summary>
    /// 建立標籤與快取鍵的關聯
    /// </summary>
    private async Task SetTagAssociationsAsync(string key, string[] tags, TimeSpan expiry)
    {
        if (_database == null) return;

        try
        {
            foreach (var tag in tags)
            {
                var taggedKeysKey = $"{TaggedKeyPrefix}{tag}";
                await _database.SetAddAsync(taggedKeysKey, key);
                await _database.KeyExpireAsync(taggedKeysKey, expiry.Add(TimeSpan.FromMinutes(5))); // 標籤集合比快取項目稍晚過期
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tag associations for key: {Key}", key);
        }
    }

    /// <summary>
    /// 移除標籤與快取鍵的關聯
    /// </summary>
    private async Task RemoveTagAssociationsAsync(string key)
    {
        if (_database == null || _redis == null) return;

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var taggedKeysPattern = $"{TaggedKeyPrefix}*";
            var taggedKeysKeys = server.Keys(pattern: taggedKeysPattern);

            foreach (var taggedKeysKey in taggedKeysKeys)
            {
                await _database.SetRemoveAsync(taggedKeysKey, key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag associations for key: {Key}", key);
        }
    }
}