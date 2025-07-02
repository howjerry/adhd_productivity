using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.DTOs;

/// <summary>
/// Data transfer object for capture items
/// </summary>
public class CaptureItemDto
{
    /// <summary>
    /// Capture item ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Captured content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of capture item
    /// </summary>
    public CaptureType Type { get; set; }

    /// <summary>
    /// Priority level
    /// </summary>
    public Priority Priority { get; set; }

    /// <summary>
    /// Is processed
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Processed date
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Context
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Associated task ID
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Associated task title
    /// </summary>
    public string? TaskTitle { get; set; }

    /// <summary>
    /// Energy level when captured
    /// </summary>
    public int EnergyLevel { get; set; }

    /// <summary>
    /// Mood when captured
    /// </summary>
    public string? Mood { get; set; }

    /// <summary>
    /// Is urgent
    /// </summary>
    public bool IsUrgent { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}