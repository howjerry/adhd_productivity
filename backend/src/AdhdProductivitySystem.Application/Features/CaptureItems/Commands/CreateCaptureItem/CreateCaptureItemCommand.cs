using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.CaptureItems.Commands.CreateCaptureItem;

/// <summary>
/// Command to create a new capture item
/// </summary>
public class CreateCaptureItemCommand : IRequest<CaptureItemDto>
{
    /// <summary>
    /// Captured content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of capture item
    /// </summary>
    public CaptureType Type { get; set; } = CaptureType.Thought;

    /// <summary>
    /// Priority level
    /// </summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>
    /// Tags
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Context where the item was captured
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Energy level when captured (1-10)
    /// </summary>
    public int EnergyLevel { get; set; } = 5;

    /// <summary>
    /// Mood when captured
    /// </summary>
    public string? Mood { get; set; }

    /// <summary>
    /// Is urgent
    /// </summary>
    public bool IsUrgent { get; set; } = false;
}