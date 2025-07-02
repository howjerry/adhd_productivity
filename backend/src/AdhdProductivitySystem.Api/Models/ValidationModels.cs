using AdhdProductivitySystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AdhdProductivitySystem.Api.Models;

/// <summary>
/// Request model for user registration with enhanced validation
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-\.\']+$", ErrorMessage = "Name contains invalid characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ADHD type is required")]
    public AdhdType AdhdType { get; set; }

    [MaxLength(50, ErrorMessage = "TimeZone cannot exceed 50 characters")]
    public string? TimeZone { get; set; }
}

/// <summary>
/// Request model for user login with enhanced validation
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional device identifier for tracking login sessions
    /// </summary>
    [MaxLength(100, ErrorMessage = "Device ID cannot exceed 100 characters")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Optional device name for user-friendly session management
    /// </summary>
    [MaxLength(100, ErrorMessage = "Device name cannot exceed 100 characters")]
    public string? DeviceName { get; set; }
}

/// <summary>
/// Request model for token refresh with validation
/// </summary>
public class RefreshRequest
{
    [Required(ErrorMessage = "Access token is required")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response model for authentication operations
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// Response model for token refresh
/// </summary>
public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// User information model
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AdhdType AdhdType { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public Theme PreferredTheme { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime LastActiveAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for password change
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "New password cannot exceed 128 characters")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for password reset
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request model for confirming password reset
/// </summary>
public class ConfirmResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset token is required")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "New password cannot exceed 128 characters")]
    public string NewPassword { get; set; } = string.Empty;
}