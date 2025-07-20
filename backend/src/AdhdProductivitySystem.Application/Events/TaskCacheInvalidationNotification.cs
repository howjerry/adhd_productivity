using MediatR;

namespace AdhdProductivitySystem.Application.Events;

/// <summary>
/// 任務快取失效通知事件
/// </summary>
public class TaskCacheInvalidationNotification : INotification
{
    public int UserId { get; }
    public int? TaskId { get; }
    public int? ParentTaskId { get; }
    public CacheInvalidationType Type { get; }

    public TaskCacheInvalidationNotification(
        int userId, 
        CacheInvalidationType type, 
        int? taskId = null, 
        int? parentTaskId = null)
    {
        UserId = userId;
        Type = type;
        TaskId = taskId;
        ParentTaskId = parentTaskId;
    }
}

/// <summary>
/// 快取失效類型
/// </summary>
public enum CacheInvalidationType
{
    TaskCreated,
    TaskUpdated,
    TaskDeleted,
    SubTaskChanged
}