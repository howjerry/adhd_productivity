using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Entities;

namespace AdhdProductivitySystem.Domain.Events;

/// <summary>
/// 任務更新事件
/// </summary>
public class TaskUpdatedEvent : IDomainEvent
{
    public TaskItem Task { get; }
    public TaskItem? PreviousState { get; }
    public DateTime OccurredAt { get; }

    public TaskUpdatedEvent(TaskItem task, TaskItem? previousState = null)
    {
        Task = task;
        PreviousState = previousState;
        OccurredAt = DateTime.UtcNow;
    }
}