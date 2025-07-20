namespace AdhdProductivitySystem.Domain.Events;

/// <summary>
/// 任務刪除事件
/// </summary>
public class TaskDeletedEvent
{
    public int TaskId { get; }
    public int UserId { get; }
    public string Title { get; }
    public DateTime OccurredAt { get; }

    public TaskDeletedEvent(int taskId, int userId, string title)
    {
        TaskId = taskId;
        UserId = userId;
        Title = title;
        OccurredAt = DateTime.UtcNow;
    }
}