namespace AdhdProductivitySystem.Domain.Enums;

/// <summary>
/// Types of captured items in the brain dump system
/// </summary>
public enum CaptureType
{
    /// <summary>
    /// Random thought or idea
    /// </summary>
    Thought = 0,

    /// <summary>
    /// Task that needs to be done
    /// </summary>
    Task = 1,

    /// <summary>
    /// Idea for a project or creative work
    /// </summary>
    Idea = 2,

    /// <summary>
    /// Something to remember
    /// </summary>
    Reminder = 3,

    /// <summary>
    /// Goal or aspiration
    /// </summary>
    Goal = 4,

    /// <summary>
    /// Problem or issue to solve
    /// </summary>
    Problem = 5,

    /// <summary>
    /// Question to research or answer
    /// </summary>
    Question = 6,

    /// <summary>
    /// Inspiration or motivation
    /// </summary>
    Inspiration = 7,

    /// <summary>
    /// Something to buy or purchase
    /// </summary>
    Shopping = 8,

    /// <summary>
    /// Reference or resource
    /// </summary>
    Reference = 9
}