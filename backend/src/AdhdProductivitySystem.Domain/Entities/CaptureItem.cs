using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Domain.Entities;

/// <summary>
/// Represents a captured thought, idea, or item in the brain dump system
/// </summary>
public class CaptureItem : BaseEntity
{
    /// <summary>
    /// The captured content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of the captured item
    /// </summary>
    public CaptureType Type { get; set; } = CaptureType.Thought;

    /// <summary>
    /// Priority of the captured item
    /// </summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>
    /// Whether the item has been processed
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Date when the item was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Tags associated with the capture item
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Context where the item was captured (location, situation, etc.)
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Associated task ID if the capture item was converted to a task
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Navigation property to the associated task
    /// </summary>
    public virtual TaskItem? Task { get; set; }

    /// <summary>
    /// ID of the user who captured this item
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Energy level when the item was captured (1-10)
    /// </summary>
    public int EnergyLevel { get; set; } = 5;

    /// <summary>
    /// Mood when the item was captured
    /// </summary>
    public string? Mood { get; set; }

    /// <summary>
    /// Whether this item requires immediate attention
    /// </summary>
    public bool IsUrgent { get; set; } = false;
}