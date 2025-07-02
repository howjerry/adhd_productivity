namespace AdhdProductivitySystem.Domain.Enums;

/// <summary>
/// Types of timer sessions
/// </summary>
public enum TimerType
{
    /// <summary>
    /// Pomodoro technique (25 minutes work, 5 minutes break)
    /// </summary>
    Pomodoro = 0,

    /// <summary>
    /// Short focus session (10-15 minutes)
    /// </summary>
    ShortFocus = 1,

    /// <summary>
    /// Long focus session (45-90 minutes)
    /// </summary>
    LongFocus = 2,

    /// <summary>
    /// Custom duration timer
    /// </summary>
    Custom = 3,

    /// <summary>
    /// Break timer
    /// </summary>
    Break = 4,

    /// <summary>
    /// Deep work session (2+ hours)
    /// </summary>
    DeepWork = 5
}