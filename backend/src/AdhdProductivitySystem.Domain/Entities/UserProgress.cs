using AdhdProductivitySystem.Domain.Common;

namespace AdhdProductivitySystem.Domain.Entities;

/// <summary>
/// Represents daily progress tracking for a user
/// </summary>
public class UserProgress : BaseEntity
{
    /// <summary>
    /// Date for this progress record
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of tasks completed on this date
    /// </summary>
    public int TasksCompleted { get; set; } = 0;

    /// <summary>
    /// Total minutes spent on productive work
    /// </summary>
    public int MinutesWorked { get; set; } = 0;

    /// <summary>
    /// Number of pomodoro sessions completed
    /// </summary>
    public int PomodoroSessions { get; set; } = 0;

    /// <summary>
    /// Number of capture items processed
    /// </summary>
    public int CaptureItemsProcessed { get; set; } = 0;

    /// <summary>
    /// Overall mood for the day (1-10)
    /// </summary>
    public int Mood { get; set; } = 5;

    /// <summary>
    /// Average energy level for the day (1-10)
    /// </summary>
    public int EnergyLevel { get; set; } = 5;

    /// <summary>
    /// Average focus level for the day (1-10)
    /// </summary>
    public int FocusLevel { get; set; } = 5;

    /// <summary>
    /// Stress level for the day (1-10)
    /// </summary>
    public int StressLevel { get; set; } = 5;

    /// <summary>
    /// Sleep quality for the previous night (1-10)
    /// </summary>
    public int SleepQuality { get; set; } = 5;

    /// <summary>
    /// Number of hours slept
    /// </summary>
    public decimal HoursSlept { get; set; } = 8;

    /// <summary>
    /// Notes about the day
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// What went well today
    /// </summary>
    public string? WentWell { get; set; }

    /// <summary>
    /// What could be improved
    /// </summary>
    public string? ToImprove { get; set; }

    /// <summary>
    /// Goals for tomorrow
    /// </summary>
    public string? TomorrowGoals { get; set; }

    /// <summary>
    /// Whether medication was taken (for users who take ADHD medication)
    /// </summary>
    public bool MedicationTaken { get; set; } = false;

    /// <summary>
    /// Exercise minutes for the day
    /// </summary>
    public int ExerciseMinutes { get; set; } = 0;

    /// <summary>
    /// ID of the user this progress belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;
}