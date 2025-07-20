using AdhdProductivitySystem.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AdhdProductivitySystem.Infrastructure.Services;

/// <summary>
/// 帶有 Prometheus 指標收集的快取服務裝飾器
/// 使用裝飾器模式來添加指標收集功能而不修改原有代碼
/// </summary>
public class CacheServiceWithMetrics : ICacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheServiceWithMetrics> _logger;

    public CacheServiceWithMetrics(ICacheService cacheService, ILogger<CacheServiceWithMetrics> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(key);
        
        try
        {
            var result = await _cacheService.GetAsync<T>(key, cancellationToken);
            
            stopwatch.Stop();
            
            // 記錄指標
            if (result != null)
            {
                // 快取命中
                IncrementCacheHits(cacheKeyType);
                _logger.LogDebug("快取命中 - Key: {Key}, Type: {Type}, Duration: {Duration}ms", 
                    key, cacheKeyType, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // 快取未命中
                IncrementCacheMisses(cacheKeyType);
                _logger.LogDebug("快取未命中 - Key: {Key}, Type: {Type}, Duration: {Duration}ms", 
                    key, cacheKeyType, stopwatch.ElapsedMilliseconds);
            }
            
            RecordCacheOperationDuration("get", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("get", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取取得異常 - Key: {Key}, Type: {Type}", key, cacheKeyType);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(key);
        
        try
        {
            await _cacheService.SetAsync(key, value, expiry, tags, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("set", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取設定成功 - Key: {Key}, Type: {Type}, Duration: {Duration}ms", 
                key, cacheKeyType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("set", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取設定異常 - Key: {Key}, Type: {Type}", key, cacheKeyType);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(key);
        
        try
        {
            await _cacheService.RemoveAsync(key, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("remove", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取移除成功 - Key: {Key}, Type: {Type}, Duration: {Duration}ms", 
                key, cacheKeyType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("remove", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取移除異常 - Key: {Key}, Type: {Type}", key, cacheKeyType);
            throw;
        }
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(pattern);
        
        try
        {
            await _cacheService.RemovePatternAsync(pattern, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("remove_pattern", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取模式移除成功 - Pattern: {Pattern}, Type: {Type}, Duration: {Duration}ms", 
                pattern, cacheKeyType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("remove_pattern", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取模式移除異常 - Pattern: {Pattern}, Type: {Type}", pattern, cacheKeyType);
            throw;
        }
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _cacheService.InvalidateByTagAsync(tag, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("invalidate_tag", tag, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取標籤失效成功 - Tag: {Tag}, Duration: {Duration}ms", 
                tag, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("invalidate_tag", tag, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取標籤失效異常 - Tag: {Tag}", tag);
            throw;
        }
    }

    public async Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var tagList = string.Join(",", tags);
        
        try
        {
            await _cacheService.InvalidateByTagsAsync(tags, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("invalidate_tags", "multiple", stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取多標籤失效成功 - Tags: {Tags}, Duration: {Duration}ms", 
                tagList, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("invalidate_tags", "multiple", stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取多標籤失效異常 - Tags: {Tags}", tagList);
            throw;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(key);
        
        try
        {
            var result = await _cacheService.GetOrSetAsync(key, factory, expiry, tags, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("get_or_set", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取 GetOrSet 成功 - Key: {Key}, Type: {Type}, Duration: {Duration}ms", 
                key, cacheKeyType, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("get_or_set", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取 GetOrSet 異常 - Key: {Key}, Type: {Type}", key, cacheKeyType);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(key);
        
        try
        {
            var result = await _cacheService.ExistsAsync(key, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("exists", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("exists", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取存在檢查異常 - Key: {Key}, Type: {Type}", key, cacheKeyType);
            throw;
        }
    }

    public async Task ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKeyType = GetCacheKeyType(key);
        
        try
        {
            await _cacheService.ExpireAsync(key, expiry, cancellationToken);
            
            stopwatch.Stop();
            RecordCacheOperationDuration("expire", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogDebug("快取過期設定成功 - Key: {Key}, Type: {Type}, Duration: {Duration}ms", 
                key, cacheKeyType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordCacheOperationDuration("expire", cacheKeyType, stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogError(ex, "快取過期設定異常 - Key: {Key}, Type: {Type}", key, cacheKeyType);
            throw;
        }
    }

    /// <summary>
    /// 從快取鍵推斷快取類型
    /// </summary>
    private static string GetCacheKeyType(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "unknown";

        // 根據鍵的前綴判斷類型
        if (key.StartsWith("tasks:", StringComparison.OrdinalIgnoreCase))
            return "tasks";
        
        if (key.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
            return "user";
        
        if (key.StartsWith("capture:", StringComparison.OrdinalIgnoreCase))
            return "capture";
        
        if (key.StartsWith("timer:", StringComparison.OrdinalIgnoreCase))
            return "timer";
        
        if (key.StartsWith("auth:", StringComparison.OrdinalIgnoreCase))
            return "auth";

        // 根據 . 分隔符判斷
        var parts = key.Split('.');
        if (parts.Length > 0)
        {
            var firstPart = parts[0].ToLowerInvariant();
            return firstPart switch
            {
                "tasks" or "task" => "tasks",
                "users" or "user" => "user",
                "capture" or "captures" => "capture",
                "timer" or "timers" => "timer",
                "auth" or "authentication" => "auth",
                _ => "other"
            };
        }

        return "other";
    }

    /// <summary>
    /// 記錄快取命中指標（需要實際的指標收集實作）
    /// </summary>
    private static void IncrementCacheHits(string cacheKeyType)
    {
        // 這裡應該呼叫實際的 Prometheus 指標收集
        // 由於跨專案引用的問題，這裡使用反射或依賴注入
        try
        {
            var metricsType = Type.GetType("AdhdProductivitySystem.Api.Services.MetricsService, AdhdProductivitySystem.Api");
            var method = metricsType?.GetMethod("IncrementCacheHits", new[] { typeof(string) });
            method?.Invoke(null, new object[] { cacheKeyType });
        }
        catch
        {
            // 如果指標收集失敗，不影響主要功能
        }
    }

    /// <summary>
    /// 記錄快取未命中指標
    /// </summary>
    private static void IncrementCacheMisses(string cacheKeyType)
    {
        try
        {
            var metricsType = Type.GetType("AdhdProductivitySystem.Api.Services.MetricsService, AdhdProductivitySystem.Api");
            var method = metricsType?.GetMethod("IncrementCacheMisses", new[] { typeof(string) });
            method?.Invoke(null, new object[] { cacheKeyType });
        }
        catch
        {
            // 如果指標收集失敗，不影響主要功能
        }
    }

    /// <summary>
    /// 記錄快取操作持續時間指標
    /// </summary>
    private static void RecordCacheOperationDuration(string operation, string cacheKeyType, double durationSeconds)
    {
        try
        {
            var metricsType = Type.GetType("AdhdProductivitySystem.Api.Services.MetricsService, AdhdProductivitySystem.Api");
            var method = metricsType?.GetMethod("RecordCacheOperationDuration", 
                new[] { typeof(string), typeof(string), typeof(double) });
            method?.Invoke(null, new object[] { operation, cacheKeyType, durationSeconds });
        }
        catch
        {
            // 如果指標收集失敗，不影響主要功能
        }
    }
}