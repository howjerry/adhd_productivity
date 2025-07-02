using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Domain.Entities;

/// <summary>
/// Represents a timer session (Pomodoro, focus session, etc.)
/// </summary>
public class TimerSession : BaseEntity
{
    /// <summary>
    /// Type of timer session
    /// </summary>
    public TimerType Type { get; set; } = TimerType.Pomodoro;

    /// <summary>
    /// Planned duration in minutes
    /// </summary>
    public int PlannedMinutes { get; set; } = 25;

    /// <summary>
    /// Actual duration in minutes
    /// </summary>
    public int ActualMinutes { get; set; } = 0;

    /// <summary>
    /// When the session started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the session ended
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Current status of the session
    /// </summary>
    public TimerStatus Status { get; set; } = TimerStatus.NotStarted;

    /// <summary>
    /// Whether the session was completed successfully
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Notes about the session
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Number of interruptions during the session
    /// </summary>
    public int Interruptions { get; set; } = 0;

    /// <summary>
    /// Focus level during the session (1-10)
    /// </summary>
    public int? FocusLevel { get; set; }

    /// <summary>
    /// Energy level at the start of the session (1-10)
    /// </summary>
    public int? StartEnergyLevel { get; set; }

    /// <summary>
    /// Energy level at the end of the session (1-10)
    /// </summary>
    public int? EndEnergyLevel { get; set; }

    /// <summary>
    /// Associated task ID if this session was for a specific task
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Navigation property to the associated task
    /// </summary>
    public virtual TaskItem? Task { get; set; }

    /// <summary>
    /// ID of the user who ran this session
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Tags associated with the session
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// What was accomplished during the session
    /// </summary>
    public string? Accomplishments { get; set; }

    /// <summary>
    /// Challenges faced during the session
    /// </summary>
    public string? Challenges { get; set; }
}