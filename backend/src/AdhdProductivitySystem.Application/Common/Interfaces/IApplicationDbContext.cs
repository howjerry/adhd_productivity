using AdhdProductivitySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// Interface for the application database context
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Users in the system
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Tasks in the system
    /// </summary>
    DbSet<TaskItem> Tasks { get; }

    /// <summary>
    /// Captured items (brain dump)
    /// </summary>
    DbSet<CaptureItem> CaptureItems { get; }

    /// <summary>
    /// Time blocks for scheduling
    /// </summary>
    DbSet<TimeBlock> TimeBlocks { get; }

    /// <summary>
    /// User progress tracking
    /// </summary>
    DbSet<UserProgress> UserProgress { get; }

    /// <summary>
    /// Timer sessions
    /// </summary>
    DbSet<TimerSession> TimerSessions { get; }

    /// <summary>
    /// Refresh tokens for authentication
    /// </summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}