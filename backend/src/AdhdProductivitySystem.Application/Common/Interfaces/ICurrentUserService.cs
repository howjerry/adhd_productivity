namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Checks if the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}