namespace AdhdProductivitySystem.Domain.Enums;

/// <summary>
/// Types of ADHD for personalized features
/// </summary>
public enum AdhdType
{
    /// <summary>
    /// Primarily inattentive presentation
    /// </summary>
    Inattentive = 0,

    /// <summary>
    /// Primarily hyperactive-impulsive presentation
    /// </summary>
    Hyperactive = 1,

    /// <summary>
    /// Combined presentation (both inattentive and hyperactive-impulsive)
    /// </summary>
    Combined = 2,

    /// <summary>
    /// Not specified or unknown
    /// </summary>
    NotSpecified = 3
}