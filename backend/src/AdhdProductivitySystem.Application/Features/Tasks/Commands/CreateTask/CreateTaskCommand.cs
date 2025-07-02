using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Command to create a new task
/// </summary>
public class CreateTaskCommand : IRequest<TaskDto>
{
    /// <summary>
    /// Task title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Task description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Task priority
    /// </summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>
    /// Estimated minutes to complete
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Parent task ID for subtasks
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    /// <summary>
    /// Is recurring task
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Recurrence pattern
    /// </summary>
    public string? RecurrencePattern { get; set; }
}