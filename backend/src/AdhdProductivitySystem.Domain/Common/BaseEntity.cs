namespace AdhdProductivitySystem.Domain.Common;

/// <summary>
/// Base entity class with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who created this entity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// ID of the user who last updated this entity
    /// </summary>
    public string? UpdatedBy { get; set; }
}