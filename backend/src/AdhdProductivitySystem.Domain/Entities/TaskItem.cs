using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Domain.Entities;

/// <summary>
/// Represents a task in the productivity system
/// </summary>
public class TaskItem : BaseEntity
{
    /// <summary>
    /// Task title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the task
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the task
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    /// <summary>
    /// Priority level of the task
    /// </summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>
    /// Estimated time to complete the task in minutes
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Actual time spent on the task in minutes
    /// </summary>
    public int ActualMinutes { get; set; } = 0;

    /// <summary>
    /// Due date for the task
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Date when the task was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Tags associated with the task
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Notes about the task
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the task is recurring
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Recurrence pattern (daily, weekly, monthly, etc.)
    /// </summary>
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// Next occurrence date for recurring tasks
    /// </summary>
    public DateTime? NextOccurrence { get; set; }

    /// <summary>
    /// ID of the user who owns this task
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// ID of the parent task (for subtasks)
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    /// <summary>
    /// Navigation property to the parent task
    /// </summary>
    public virtual TaskItem? ParentTask { get; set; }

    /// <summary>
    /// Child tasks (subtasks)
    /// </summary>
    public virtual ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();

    /// <summary>
    /// Timer sessions associated with this task
    /// </summary>
    public virtual ICollection<TimerSession> TimerSessions { get; set; } = new List<TimerSession>();
}