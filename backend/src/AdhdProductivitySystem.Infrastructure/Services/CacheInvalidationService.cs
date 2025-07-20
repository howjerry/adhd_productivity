using AdhdProductivitySystem.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdhdProductivitySystem.Infrastructure.Services;

/// <summary>
/// 快取失效服務實作，實現智慧快取失效策略
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvalidateOnTaskCreatedAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 當建立新任務時，失效使用者的任務列表快取
            await InvalidateUserTaskCacheAsync(userId, cancellationToken);
            
            _logger.LogDebug("Cache invalidated for task creation, user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache on task created for user: {UserId}", userId);
        }
    }

    public async Task InvalidateOnTaskUpdatedAsync(int userId, int taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 任務更新時，失效相關快取
            await Task.WhenAll(
                InvalidateUserTaskCacheAsync(userId, cancellationToken),
                InvalidateTaskDetailCacheAsync(taskId, cancellationToken)
            );
            
            _logger.LogDebug("Cache invalidated for task update, user: {UserId}, task: {TaskId}", userId, taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache on task updated for user: {UserId}, task: {TaskId}", userId, taskId);
        }
    }

    public async Task InvalidateOnTaskDeletedAsync(int userId, int taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 任務刪除時，失效相關快取
            await Task.WhenAll(
                InvalidateUserTaskCacheAsync(userId, cancellationToken),
                InvalidateTaskDetailCacheAsync(taskId, cancellationToken)
            );
            
            _logger.LogDebug("Cache invalidated for task deletion, user: {UserId}, task: {TaskId}", userId, taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache on task deleted for user: {UserId}, task: {TaskId}", userId, taskId);
        }
    }

    public async Task InvalidateOnSubTaskChangedAsync(int userId, int? parentTaskId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 子任務變動時，失效父任務和使用者任務列表快取
            var tasks = new List<Task>
            {
                InvalidateUserTaskCacheAsync(userId, cancellationToken)
            };

            if (parentTaskId.HasValue)
            {
                tasks.Add(InvalidateTaskDetailCacheAsync(parentTaskId.Value, cancellationToken));
            }

            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Cache invalidated for subtask change, user: {UserId}, parent task: {ParentTaskId}", 
                userId, parentTaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache on subtask changed for user: {UserId}, parent task: {ParentTaskId}", 
                userId, parentTaskId);
        }
    }

    public async Task InvalidateUserTaskCacheAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 失效使用者的所有任務查詢快取
            var userTag = $"user:{userId}";
            await _cacheService.InvalidateByTagAsync(userTag, cancellationToken);
            
            // 同時失效任務列表相關的模式快取
            var taskPattern = $"tasks:user:{userId}:*";
            await _cacheService.RemovePatternAsync(taskPattern, cancellationToken);
            
            _logger.LogDebug("User task cache invalidated for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user task cache for user: {UserId}", userId);
        }
    }

    public async Task InvalidateTaskDetailCacheAsync(int taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 失效特定任務的詳細資訊快取
            var taskDetailKey = $"task:detail:{taskId}";
            await _cacheService.RemoveAsync(taskDetailKey, cancellationToken);
            
            // 失效任務相關的模式快取
            var taskPattern = $"task:{taskId}:*";
            await _cacheService.RemovePatternAsync(taskPattern, cancellationToken);
            
            _logger.LogDebug("Task detail cache invalidated for task: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating task detail cache for task: {TaskId}", taskId);
        }
    }
}