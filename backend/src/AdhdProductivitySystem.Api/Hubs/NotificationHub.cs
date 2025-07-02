using AdhdProductivitySystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AdhdProductivitySystem.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IApplicationDbContext context, ILogger<NotificationHub> logger)
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
            _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
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
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="notification">Notification data</param>
    public async Task SendNotificationToUser(Guid userId, object notification)
    {
        try
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Notification sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }

    /// <summary>
    /// Send a reminder notification
    /// </summary>
    /// <param name="reminderData">Reminder data</param>
    public async Task SendReminder(object reminderData)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var notification = new
            {
                Type = "reminder",
                Data = reminderData,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("ReceiveReminder", notification);
            _logger.LogInformation("Reminder sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder");
        }
    }

    /// <summary>
    /// Send a task update notification
    /// </summary>
    /// <param name="taskData">Task update data</param>
    public async Task SendTaskUpdate(object taskData)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var notification = new
            {
                Type = "task_update",
                Data = taskData,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("ReceiveTaskUpdate", notification);
            _logger.LogInformation("Task update sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending task update");
        }
    }

    /// <summary>
    /// Send a motivation notification
    /// </summary>
    /// <param name="message">Motivational message</param>
    /// <param name="type">Motivation type</param>
    public async Task SendMotivation(string message, string type = "general")
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var notification = new
            {
                Type = "motivation",
                Data = new
                {
                    Message = message,
                    MotivationType = type
                },
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("ReceiveMotivation", notification);
            _logger.LogInformation("Motivation sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending motivation");
        }
    }

    /// <summary>
    /// Send a break reminder
    /// </summary>
    /// <param name="breakType">Type of break (short, long, etc.)</param>
    /// <param name="durationMinutes">Suggested break duration</param>
    public async Task SendBreakReminder(string breakType, int durationMinutes)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var notification = new
            {
                Type = "break_reminder",
                Data = new
                {
                    BreakType = breakType,
                    DurationMinutes = durationMinutes,
                    Suggestions = GetBreakSuggestions(breakType)
                },
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("ReceiveBreakReminder", notification);
            _logger.LogInformation("Break reminder sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending break reminder");
        }
    }

    /// <summary>
    /// Send a focus alert
    /// </summary>
    /// <param name="alertType">Type of focus alert</param>
    /// <param name="message">Alert message</param>
    public async Task SendFocusAlert(string alertType, string message)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var notification = new
            {
                Type = "focus_alert",
                Data = new
                {
                    AlertType = alertType,
                    Message = message
                },
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group($"User_{userId}").SendAsync("ReceiveFocusAlert", notification);
            _logger.LogInformation("Focus alert sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending focus alert");
        }
    }

    /// <summary>
    /// Mark notifications as read
    /// </summary>
    /// <param name="notificationIds">IDs of notifications to mark as read</param>
    public async Task MarkNotificationsAsRead(Guid[] notificationIds)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            // In a real implementation, you'd update the database here
            await Clients.Caller.SendAsync("NotificationsMarkedAsRead", notificationIds);
            _logger.LogInformation("Notifications marked as read for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notifications as read");
        }
    }

    /// <summary>
    /// Join a notification group (for specific types of notifications)
    /// </summary>
    /// <param name="groupName">Group name to join</param>
    public async Task JoinNotificationGroup(string groupName)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined notification group {GroupName}", userId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining notification group");
        }
    }

    /// <summary>
    /// Leave a notification group
    /// </summary>
    /// <param name="groupName">Group name to leave</param>
    public async Task LeaveNotificationGroup(string groupName)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} left notification group {GroupName}", userId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving notification group");
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

    /// <summary>
    /// Gets break suggestions based on break type
    /// </summary>
    /// <param name="breakType">Type of break</param>
    /// <returns>List of break suggestions</returns>
    private string[] GetBreakSuggestions(string breakType)
    {
        return breakType.ToLower() switch
        {
            "short" => new[]
            {
                "Take 5 deep breaths",
                "Stretch your neck and shoulders",
                "Look away from the screen for 20 seconds",
                "Drink a glass of water",
                "Do some quick desk exercises"
            },
            "long" => new[]
            {
                "Take a 15-20 minute walk",
                "Do some light stretching or yoga",
                "Have a healthy snack",
                "Step outside for fresh air",
                "Do a brief meditation session"
            },
            "pomodoro" => new[]
            {
                "Take a 5-minute walk",
                "Do some breathing exercises",
                "Stretch your body",
                "Tidy up your workspace",
                "Hydrate yourself"
            },
            _ => new[]
            {
                "Take a moment to relax",
                "Stretch your body",
                "Take some deep breaths",
                "Rest your eyes"
            }
        };
    }
}