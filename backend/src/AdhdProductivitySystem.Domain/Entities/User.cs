using AdhdProductivitySystem.Domain.Common;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Domain.Entities;

/// <summary>
/// Represents a user in the ADHD productivity system
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's email address (unique identifier)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password for authentication
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for password hashing
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// User's ADHD type for personalized features
    /// </summary>
    public AdhdType AdhdType { get; set; } = AdhdType.Combined;

    /// <summary>
    /// User's timezone for proper time calculations
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// User's preferred theme
    /// </summary>
    public Theme PreferredTheme { get; set; } = Theme.Light;

    /// <summary>
    /// Whether the user has completed onboarding
    /// </summary>
    public bool IsOnboardingCompleted { get; set; } = false;

    /// <summary>
    /// Last time the user was active
    /// </summary>
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User's profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// User's tasks
    /// </summary>
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    /// <summary>
    /// User's capture items
    /// </summary>
    public virtual ICollection<CaptureItem> CaptureItems { get; set; } = new List<CaptureItem>();

    /// <summary>
    /// User's time blocks
    /// </summary>
    public virtual ICollection<TimeBlock> TimeBlocks { get; set; } = new List<TimeBlock>();

    /// <summary>
    /// User's progress records
    /// </summary>
    public virtual ICollection<UserProgress> ProgressRecords { get; set; } = new List<UserProgress>();

    /// <summary>
    /// User's timer sessions
    /// </summary>
    public virtual ICollection<TimerSession> TimerSessions { get; set; } = new List<TimerSession>();

    /// <summary>
    /// User's refresh tokens
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}