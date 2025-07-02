using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Domain.Entities;

/// <summary>
/// Represents a time block for scheduling and time management
/// </summary>
public class TimeBlock : BaseEntity
{
    /// <summary>
    /// Title of the time block
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of what should be done during this time block
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start time of the time block
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the time block
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Type of the time block
    /// </summary>
    public TimeBlockType Type { get; set; } = TimeBlockType.Work;

    /// <summary>
    /// Color for visual representation
    /// </summary>
    public string Color { get; set; } = "#3498db";

    /// <summary>
    /// Whether this time block is recurring
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Recurrence pattern for recurring time blocks
    /// </summary>
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// Whether the time block is flexible (can be moved if needed)
    /// </summary>
    public bool IsFlexible { get; set; } = true;

    /// <summary>
    /// Associated task ID if this time block is for a specific task
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Navigation property to the associated task
    /// </summary>
    public virtual TaskItem? Task { get; set; }

    /// <summary>
    /// ID of the user who owns this time block
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Whether the time block was completed as planned
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Notes about how the time block went
    /// </summary>
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Actual start time (may differ from planned start time)
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// Actual end time (may differ from planned end time)
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// Energy level during this time block (1-10)
    /// </summary>
    public int? EnergyLevel { get; set; }

    /// <summary>
    /// Focus level during this time block (1-10)
    /// </summary>
    public int? FocusLevel { get; set; }
}