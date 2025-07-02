namespace AdhdProductivitySystem.Application.Common.DTOs;

/// <summary>
/// Data transfer object for user progress
/// </summary>
public class UserProgressDto
{
    /// <summary>
    /// Progress record ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date for this progress record
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Tasks completed
    /// </summary>
    public int TasksCompleted { get; set; }

    /// <summary>
    /// Minutes worked
    /// </summary>
    public int MinutesWorked { get; set; }

    /// <summary>
    /// Pomodoro sessions completed
    /// </summary>
    public int PomodoroSessions { get; set; }

    /// <summary>
    /// Capture items processed
    /// </summary>
    public int CaptureItemsProcessed { get; set; }

    /// <summary>
    /// Overall mood (1-10)
    /// </summary>
    public int Mood { get; set; }

    /// <summary>
    /// Energy level (1-10)
    /// </summary>
    public int EnergyLevel { get; set; }

    /// <summary>
    /// Focus level (1-10)
    /// </summary>
    public int FocusLevel { get; set; }

    /// <summary>
    /// Stress level (1-10)
    /// </summary>
    public int StressLevel { get; set; }

    /// <summary>
    /// Sleep quality (1-10)
    /// </summary>
    public int SleepQuality { get; set; }

    /// <summary>
    /// Hours slept
    /// </summary>
    public decimal HoursSlept { get; set; }

    /// <summary>
    /// Daily notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// What went well
    /// </summary>
    public string? WentWell { get; set; }

    /// <summary>
    /// What to improve
    /// </summary>
    public string? ToImprove { get; set; }

    /// <summary>
    /// Tomorrow's goals
    /// </summary>
    public string? TomorrowGoals { get; set; }

    /// <summary>
    /// Medication taken
    /// </summary>
    public bool MedicationTaken { get; set; }

    /// <summary>
    /// Exercise minutes
    /// </summary>
    public int ExerciseMinutes { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}