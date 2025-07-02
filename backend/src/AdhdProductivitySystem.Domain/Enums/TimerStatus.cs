namespace AdhdProductivitySystem.Domain.Enums;

/// <summary>
/// Status of a timer session
/// </summary>
public enum TimerStatus
{
    /// <summary>
    /// Timer has not started yet
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Timer is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Timer is paused
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Timer completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Timer was stopped before completion
    /// </summary>
    Stopped = 4,

    /// <summary>
    /// Timer was interrupted
    /// </summary>
    Interrupted = 5
}