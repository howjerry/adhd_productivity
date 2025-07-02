namespace AdhdProductivitySystem.Domain.Enums;

/// <summary>
/// Status of a task
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is not started
    /// </summary>
    Todo = 0,

    /// <summary>
    /// Task is in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Task is completed
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Task is on hold
    /// </summary>
    OnHold = 3,

    /// <summary>
    /// Task is cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Task is waiting for something external
    /// </summary>
    Waiting = 5
}