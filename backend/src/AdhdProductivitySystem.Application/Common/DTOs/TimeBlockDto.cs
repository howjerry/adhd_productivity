using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.DTOs;

/// <summary>
/// Data transfer object for time blocks
/// </summary>
public class TimeBlockDto
{
    /// <summary>
    /// Time block ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title of the time block
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Time block type
    /// </summary>
    public TimeBlockType Type { get; set; }

    /// <summary>
    /// Color for visual representation
    /// </summary>
    public string Color { get; set; } = "#3498db";

    /// <summary>
    /// Is recurring
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Recurrence pattern
    /// </summary>
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// Is flexible
    /// </summary>
    public bool IsFlexible { get; set; }

    /// <summary>
    /// Associated task ID
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Associated task title
    /// </summary>
    public string? TaskTitle { get; set; }

    /// <summary>
    /// Is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Completion notes
    /// </summary>
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Actual start time
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// Actual end time
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// Energy level (1-10)
    /// </summary>
    public int? EnergyLevel { get; set; }

    /// <summary>
    /// Focus level (1-10)
    /// </summary>
    public int? FocusLevel { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}