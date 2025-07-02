namespace AdhdProductivitySystem.Domain.Enums;

/// <summary>
/// Types of time blocks for scheduling
/// </summary>
public enum TimeBlockType
{
    /// <summary>
    /// Work-related time block
    /// </summary>
    Work = 0,

    /// <summary>
    /// Personal time block
    /// </summary>
    Personal = 1,

    /// <summary>
    /// Break or rest time
    /// </summary>
    Break = 2,

    /// <summary>
    /// Exercise or physical activity
    /// </summary>
    Exercise = 3,

    /// <summary>
    /// Meeting or appointment
    /// </summary>
    Meeting = 4,

    /// <summary>
    /// Learning or education
    /// </summary>
    Learning = 5,

    /// <summary>
    /// Creative work or hobby
    /// </summary>
    Creative = 6,

    /// <summary>
    /// Meal time
    /// </summary>
    Meal = 7,

    /// <summary>
    /// Sleep or rest
    /// </summary>
    Sleep = 8,

    /// <summary>
    /// Travel or commute
    /// </summary>
    Travel = 9,

    /// <summary>
    /// Buffer time for transitions
    /// </summary>
    Buffer = 10
}