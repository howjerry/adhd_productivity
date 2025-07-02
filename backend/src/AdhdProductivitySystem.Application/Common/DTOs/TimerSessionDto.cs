using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.DTOs;

/// <summary>
/// Data transfer object for timer sessions
/// </summary>
public class TimerSessionDto
{
    /// <summary>
    /// Timer session ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timer type
    /// </summary>
    public TimerType Type { get; set; }

    /// <summary>
    /// Planned duration in minutes
    /// </summary>
    public int PlannedMinutes { get; set; }

    /// <summary>
    /// Actual duration in minutes
    /// </summary>
    public int ActualMinutes { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Timer status
    /// </summary>
    public TimerStatus Status { get; set; }

    /// <summary>
    /// Is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Session notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Number of interruptions
    /// </summary>
    public int Interruptions { get; set; }

    /// <summary>
    /// Focus level (1-10)
    /// </summary>
    public int? FocusLevel { get; set; }

    /// <summary>
    /// Start energy level (1-10)
    /// </summary>
    public int? StartEnergyLevel { get; set; }

    /// <summary>
    /// End energy level (1-10)
    /// </summary>
    public int? EndEnergyLevel { get; set; }

    /// <summary>
    /// Associated task ID
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Associated task title
    /// </summary>
    public string? TaskTitle { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Accomplishments
    /// </summary>
    public string? Accomplishments { get; set; }

    /// <summary>
    /// Challenges
    /// </summary>
    public string? Challenges { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}