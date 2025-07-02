using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.DTOs;

/// <summary>
/// Data transfer object for users
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ADHD type
    /// </summary>
    public AdhdType AdhdType { get; set; }

    /// <summary>
    /// Timezone
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Preferred theme
    /// </summary>
    public Theme PreferredTheme { get; set; }

    /// <summary>
    /// Onboarding completed
    /// </summary>
    public bool IsOnboardingCompleted { get; set; }

    /// <summary>
    /// Last active timestamp
    /// </summary>
    public DateTime LastActiveAt { get; set; }

    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}