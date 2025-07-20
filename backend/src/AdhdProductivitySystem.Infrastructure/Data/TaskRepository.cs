using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using TaskStatus = AdhdProductivitySystem.Domain.Enums.TaskStatus;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// Task 特定的 Repository 實作
/// </summary>
public class TaskRepository : Repository<TaskItem>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根據使用者 ID 取得任務，包含子任務統計
    /// </summary>
    public async Task<List<TaskDto>> GetTasksWithStatisticsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.UserId == userId);

        // 應用篩選條件
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (dueDateFrom.HasValue)
        {
            query = query.Where(t => t.DueDate >= dueDateFrom.Value);
        }

        if (dueDateTo.HasValue)
        {
            query = query.Where(t => t.DueDate <= dueDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower()).ToList();
            
            // 優化標籤搜尋
            query = query.Where(t => tagList.Any(tag => t.Tags.ToLower().Contains(tag)));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchTextLower = searchText.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchTextLower) || 
                                   (t.Description != null && t.Description.ToLower().Contains(searchTextLower)));
        }

        if (!includeSubTasks)
        {
            query = query.Where(t => t.ParentTaskId == null);
        }

        // 應用排序
        query = sortBy.ToLower() switch
        {
            "title" => sortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "priority" => sortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "duedate" => sortDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "status" => sortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "updatedat" => sortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            _ => sortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };

        // 應用分頁並投影到 DTO（避免 N+1 查詢）
        var taskDtos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(task => new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                EstimatedMinutes = task.EstimatedMinutes,
                ActualMinutes = task.ActualMinutes,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                Tags = task.Tags,
                Notes = task.Notes,
                IsRecurring = task.IsRecurring,
                RecurrencePattern = task.RecurrencePattern,
                NextOccurrence = task.NextOccurrence,
                UserId = task.UserId,
                ParentTaskId = task.ParentTaskId,
                // 在資料庫中計算子任務統計（避免 N+1 查詢）
                SubTaskCount = task.SubTasks.Count(),
                CompletedSubTaskCount = task.SubTasks.Count(st => st.Status == TaskStatus.Completed),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return taskDtos;
    }

    /// <summary>
    /// 根據使用者 ID 和任務 ID 取得任務（包含子任務統計）
    /// </summary>
    public async Task<TaskDto?> GetTaskByIdWithStatisticsAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var taskDto = await _dbSet
            .Where(t => t.Id == taskId && t.UserId == userId)
            .Select(task => new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                EstimatedMinutes = task.EstimatedMinutes,
                ActualMinutes = task.ActualMinutes,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                Tags = task.Tags,
                Notes = task.Notes,
                IsRecurring = task.IsRecurring,
                RecurrencePattern = task.RecurrencePattern,
                NextOccurrence = task.NextOccurrence,
                UserId = task.UserId,
                ParentTaskId = task.ParentTaskId,
                SubTaskCount = task.SubTasks.Count(),
                CompletedSubTaskCount = task.SubTasks.Count(st => st.Status == TaskStatus.Completed),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return taskDto;
    }

    /// <summary>
    /// 取得使用者的子任務
    /// </summary>
    public async Task<List<TaskItem>> GetSubTasksAsync(Guid userId, Guid parentTaskId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.ParentTaskId == parentTaskId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 取得使用者今日任務統計
    /// </summary>
    public async Task<TodayTaskStatistics> GetTodayStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayTasks = await _dbSet
            .Where(t => t.UserId == userId && 
                       t.DueDate.HasValue && 
                       t.DueDate.Value.Date == today)
            .ToListAsync(cancellationToken);

        var overdueTasks = await _dbSet
            .Where(t => t.UserId == userId &&
                       t.Status != TaskStatus.Completed &&
                       t.DueDate.HasValue &&
                       t.DueDate.Value.Date < today)
            .CountAsync(cancellationToken);

        return new TodayTaskStatistics
        {
            TotalTasks = todayTasks.Count,
            CompletedTasks = todayTasks.Count(t => t.Status == TaskStatus.Completed),
            PendingTasks = todayTasks.Count(t => t.Status == TaskStatus.Todo || t.Status == TaskStatus.InProgress),
            OverdueTasks = overdueTasks,
            HighPriorityTasks = todayTasks.Count(t => t.Priority == Priority.High || t.Priority == Priority.Critical)
        };
    }

    /// <summary>
    /// 取得使用者的過期任務
    /// </summary>
    public async Task<List<TaskItem>> GetOverdueTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        return await _dbSet
            .Where(t => t.UserId == userId &&
                       t.Status != TaskStatus.Completed &&
                       t.DueDate.HasValue &&
                       t.DueDate.Value.Date < today)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 取得使用者的重複任務
    /// </summary>
    public async Task<List<TaskItem>> GetRecurringTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.IsRecurring)
            .OrderBy(t => t.NextOccurrence)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量更新任務狀態
    /// </summary>
    public async Task<int> BulkUpdateStatusAsync(Guid userId, List<Guid> taskIds, Domain.Enums.TaskStatus newStatus, CancellationToken cancellationToken = default)
    {
        var tasks = await _dbSet
            .Where(t => t.UserId == userId && taskIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var task in tasks)
        {
            task.Status = newStatus;
            if (newStatus == TaskStatus.Completed && !task.CompletedAt.HasValue)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (newStatus != TaskStatus.Completed)
            {
                task.CompletedAt = null;
            }
            updatedCount++;
        }

        return updatedCount;
    }

    /// <summary>
    /// 檢查使用者是否有權限存取任務
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(t => t.Id == taskId && t.UserId == userId, cancellationToken);
    }
}