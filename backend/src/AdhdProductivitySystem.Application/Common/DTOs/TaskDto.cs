using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.DTOs;

/// <summary>
/// Data transfer object for tasks
/// </summary>
public class TaskDto
{
    /// <summary>
    /// Task ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Task title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Task description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Task status
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Task priority
    /// </summary>
    public Priority Priority { get; set; }

    /// <summary>
    /// Estimated minutes to complete
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Actual minutes spent
    /// </summary>
    public int ActualMinutes { get; set; }

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Completion date
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Is recurring task
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Recurrence pattern
    /// </summary>
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// Next occurrence
    /// </summary>
    public DateTime? NextOccurrence { get; set; }

    /// <summary>
    /// Parent task ID
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    /// <summary>
    /// Number of subtasks
    /// </summary>
    public int SubTaskCount { get; set; }

    /// <summary>
    /// Number of completed subtasks
    /// </summary>
    public int CompletedSubTaskCount { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}