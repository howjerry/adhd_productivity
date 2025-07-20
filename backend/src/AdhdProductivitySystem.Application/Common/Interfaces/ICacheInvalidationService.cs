namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// 快取失效服務介面，負責智慧快取失效策略
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// 當任務被建立時失效相關快取
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateOnTaskCreatedAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 當任務被更新時失效相關快取
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="taskId">任務 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateOnTaskUpdatedAsync(int userId, int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 當任務被刪除時失效相關快取
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="taskId">任務 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateOnTaskDeletedAsync(int userId, int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 當子任務變動時失效相關快取
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="parentTaskId">父任務 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateOnSubTaskChangedAsync(int userId, int? parentTaskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效使用者的所有任務相關快取
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateUserTaskCacheAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效特定任務的詳細資訊快取
    /// </summary>
    /// <param name="taskId">任務 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateTaskDetailCacheAsync(int taskId, CancellationToken cancellationToken = default);
}