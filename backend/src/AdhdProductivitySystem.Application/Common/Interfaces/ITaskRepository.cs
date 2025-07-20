using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// Task 特定的 Repository 介面
/// </summary>
public interface ITaskRepository : IRepository<TaskItem>
{
    /// <summary>
    /// 根據使用者 ID 取得任務，包含子任務統計
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="status">狀態篩選</param>
    /// <param name="priority">優先級篩選</param>
    /// <param name="dueDateFrom">到期日起始篩選</param>
    /// <param name="dueDateTo">到期日結束篩選</param>
    /// <param name="tags">標籤篩選（逗號分隔）</param>
    /// <param name="searchText">搜尋文字</param>
    /// <param name="includeSubTasks">是否包含子任務</param>
    /// <param name="page">頁數</param>
    /// <param name="pageSize">每頁數量</param>
    /// <param name="sortBy">排序欄位</param>
    /// <param name="sortDescending">是否降序</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>任務 DTO 列表</returns>
    Task<List<TaskDto>> GetTasksWithStatisticsAsync(
        Guid userId,
        Domain.Enums.TaskStatus? status = null,
        Priority? priority = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        string? tags = null,
        string? searchText = null,
        bool includeSubTasks = true,
        int page = 1,
        int pageSize = 50,
        string sortBy = "createdat",
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據使用者 ID 和任務 ID 取得任務（包含子任務統計）
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="taskId">任務 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>任務 DTO 或 null</returns>
    Task<TaskDto?> GetTaskByIdWithStatisticsAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的子任務
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="parentTaskId">父任務 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>子任務列表</returns>
    Task<List<TaskItem>> GetSubTasksAsync(Guid userId, Guid parentTaskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者今日任務統計
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>今日任務統計</returns>
    Task<TodayTaskStatistics> GetTodayStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的過期任務
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>過期任務列表</returns>
    Task<List<TaskItem>> GetOverdueTasksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的重複任務
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>重複任務列表</returns>
    Task<List<TaskItem>> GetRecurringTasksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新任務狀態
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="taskIds">任務 ID 列表</param>
    /// <param name="newStatus">新狀態</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>更新的任務數量</returns>
    Task<int> BulkUpdateStatusAsync(Guid userId, List<Guid> taskIds, Domain.Enums.TaskStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查使用者是否有權限存取任務
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="taskId">任務 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>是否有權限</returns>
    Task<bool> HasPermissionAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 今日任務統計
/// </summary>
public class TodayTaskStatistics
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int HighPriorityTasks { get; set; }
    public double CompletionRate => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
}