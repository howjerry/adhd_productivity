using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Entities;

namespace AdhdProductivitySystem.Domain.Events;

/// <summary>
/// 任務建立事件
/// </summary>
public class TaskCreatedEvent : IDomainEvent
{
    public TaskItem Task { get; }
    public DateTime OccurredAt { get; }

    public TaskCreatedEvent(TaskItem task)
    {
        Task = task;
        OccurredAt = DateTime.UtcNow;
    }
}