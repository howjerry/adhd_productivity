using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AdhdProductivitySystem.Api.Hubs;

/// <summary>
/// SignalR hub for real-time timer functionality
/// </summary>
[Authorize]
public class TimerHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TimerHub> _logger;

    public TimerHub(IApplicationDbContext context, ILogger<TimerHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} connected to TimerHub", userId);
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} disconnected from TimerHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Start a timer session
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    /// <param name="type">Timer type</param>
    /// <param name="durationMinutes">Duration in minutes</param>
    /// <param name="taskId">Associated task ID (optional)</param>
    public async Task StartTimer(Guid sessionId, TimerType type, int durationMinutes, Guid? taskId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("TimerError", "User not authenticated");
                return;
            }

            var timerData = new
            {
                SessionId = sessionId,
                Type = type,
                DurationMinutes = durationMinutes,
                TaskId = taskId,
                StartTime = DateTime.UtcNow,
                Status = TimerStatus.Running
            };

            // Notify the user that the timer has started
            await Clients.Group($"User_{userId}").SendAsync("TimerStarted", timerData);

            _logger.LogInformation("Timer started for user {UserId}, session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting timer");
            await Clients.Caller.SendAsync("TimerError", "Failed to start timer");
        }
    }

    /// <summary>
    /// Pause a timer session
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    public async Task PauseTimer(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("TimerError", "User not authenticated");
                return;
            }

            var timerData = new
            {
                SessionId = sessionId,
                Status = TimerStatus.Paused,
                PausedAt = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("TimerPaused", timerData);

            _logger.LogInformation("Timer paused for user {UserId}, session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing timer");
            await Clients.Caller.SendAsync("TimerError", "Failed to pause timer");
        }
    }

    /// <summary>
    /// Resume a paused timer session
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    public async Task ResumeTimer(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("TimerError", "User not authenticated");
                return;
            }

            var timerData = new
            {
                SessionId = sessionId,
                Status = TimerStatus.Running,
                ResumedAt = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("TimerResumed", timerData);

            _logger.LogInformation("Timer resumed for user {UserId}, session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming timer");
            await Clients.Caller.SendAsync("TimerError", "Failed to resume timer");
        }
    }

    /// <summary>
    /// Stop a timer session
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    /// <param name="reason">Reason for stopping</param>
    public async Task StopTimer(Guid sessionId, string reason = "Manual stop")
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("TimerError", "User not authenticated");
                return;
            }

            var timerData = new
            {
                SessionId = sessionId,
                Status = TimerStatus.Stopped,
                StoppedAt = DateTime.UtcNow,
                Reason = reason
            };

            await Clients.Group($"User_{userId}").SendAsync("TimerStopped", timerData);

            _logger.LogInformation("Timer stopped for user {UserId}, session {SessionId}, reason: {Reason}", userId, sessionId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping timer");
            await Clients.Caller.SendAsync("TimerError", "Failed to stop timer");
        }
    }

    /// <summary>
    /// Complete a timer session
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    public async Task CompleteTimer(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("TimerError", "User not authenticated");
                return;
            }

            var timerData = new
            {
                SessionId = sessionId,
                Status = TimerStatus.Completed,
                CompletedAt = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("TimerCompleted", timerData);

            _logger.LogInformation("Timer completed for user {UserId}, session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing timer");
            await Clients.Caller.SendAsync("TimerError", "Failed to complete timer");
        }
    }

    /// <summary>
    /// Send timer tick update
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    /// <param name="remainingSeconds">Remaining seconds</param>
    public async Task TimerTick(Guid sessionId, int remainingSeconds)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var tickData = new
            {
                SessionId = sessionId,
                RemainingSeconds = remainingSeconds,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("TimerTick", tickData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending timer tick");
        }
    }

    /// <summary>
    /// Join a specific timer session room (for collaborative features)
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    public async Task JoinTimerSession(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("TimerError", "User not authenticated");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            await Clients.Group($"Session_{sessionId}").SendAsync("UserJoinedSession", new { UserId = userId, SessionId = sessionId });

            _logger.LogInformation("User {UserId} joined timer session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining timer session");
            await Clients.Caller.SendAsync("TimerError", "Failed to join timer session");
        }
    }

    /// <summary>
    /// Leave a timer session room
    /// </summary>
    /// <param name="sessionId">Timer session ID</param>
    public async Task LeaveTimerSession(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            await Clients.Group($"Session_{sessionId}").SendAsync("UserLeftSession", new { UserId = userId, SessionId = sessionId });

            _logger.LogInformation("User {UserId} left timer session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving timer session");
        }
    }

    /// <summary>
    /// Gets the current user's ID from the context
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}