using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// 安全監控中間件，用於檢測和記錄安全事件
/// </summary>
public class SecurityMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMonitoringMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly SecurityMonitoringOptions _options;

    // 用於追蹤可疑活動的記憶體儲存
    private static readonly ConcurrentDictionary<string, SuspiciousActivity> SuspiciousActivities = new();

    public SecurityMonitoringMiddleware(
        RequestDelegate next, 
        ILogger<SecurityMonitoringMiddleware> logger,
        IMemoryCache cache,
        SecurityMonitoringOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options ?? new SecurityMonitoringOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var requestPath = context.Request.Path.Value ?? "";

        try
        {
            // 檢查IP黑名單
            if (IsBlacklistedIp(clientIp))
            {
                await BlockRequest(context, "IP地址已被封鎖", SecurityEventType.BlockedIpAccess);
                return;
            }

            // 檢查速率限制
            if (await IsRateLimited(clientIp, context.Request.Path))
            {
                await HandleSecurityEvent(context, SecurityEventType.RateLimitExceeded, 
                    $"來自 {clientIp} 的請求超過速率限制");
                // 讓原有的速率限制中間件處理
            }

            // 檢查可疑的User-Agent
            if (IsSuspiciousUserAgent(userAgent))
            {
                await HandleSecurityEvent(context, SecurityEventType.SuspiciousUserAgent,
                    $"檢測到可疑的User-Agent: {userAgent}");
            }

            // 檢查異常請求模式
            if (HasAnomalousRequestPattern(context))
            {
                await HandleSecurityEvent(context, SecurityEventType.AnomalousRequest,
                    $"檢測到異常請求模式: {context.Request.Method} {requestPath}");
            }

            // 檢查authentication相關的安全事件
            await MonitorAuthenticationEvents(context);

            await _next(context);

            // 檢查回應狀態碼
            await MonitorResponseEvents(context, clientIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全監控中間件發生錯誤");
            await _next(context);
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = ipAddress.Split(',')[0].Trim();
        }

        ipAddress ??= context.Request.Headers["X-Real-IP"].FirstOrDefault();
        ipAddress ??= context.Connection.RemoteIpAddress?.ToString();

        return ipAddress ?? "unknown";
    }

    private bool IsBlacklistedIp(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "unknown")
            return false;

        // 檢查是否在記憶體快取的黑名單中
        return _cache.TryGetValue($"blacklist_ip_{ipAddress}", out _);
    }

    private async Task<bool> IsRateLimited(string clientIp, PathString path)
    {
        var key = $"rate_limit_{clientIp}_{path}";
        var currentCount = _cache.Get<int>(key);
        
        if (currentCount >= _options.MaxRequestsPerMinute)
        {
            return true;
        }

        // 更新計數器
        _cache.Set(key, currentCount + 1, TimeSpan.FromMinutes(1));
        return false;
    }

    private static bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return true;

        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "scanner", "curl", "wget",
            "python", "java", "go-http", "perl", "ruby", "php", "sqlmap",
            "nmap", "nikto", "burp", "owasp", "injection"
        };

        var lowerUserAgent = userAgent.ToLowerInvariant();
        return suspiciousPatterns.Any(pattern => lowerUserAgent.Contains(pattern));
    }

    private static bool HasAnomalousRequestPattern(HttpContext context)
    {
        var request = context.Request;
        
        // 檢查異常大的請求
        if (request.ContentLength > 50 * 1024 * 1024) // 50MB
            return true;

        // 檢查異常多的標頭
        if (request.Headers.Count > 50)
            return true;

        // 檢查可疑的路徑模式
        var path = request.Path.Value?.ToLowerInvariant() ?? "";
        var suspiciousPathPatterns = new[]
        {
            "admin", "wp-admin", "phpmyadmin", "config", "backup", 
            ".env", ".git", ".svn", "database", "db_backup"
        };

        return suspiciousPathPatterns.Any(pattern => path.Contains(pattern));
    }

    private async Task MonitorAuthenticationEvents(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        if (path.Contains("/auth/login") || path.Contains("/auth/register"))
        {
            var clientIp = GetClientIpAddress(context);
            var activityKey = $"auth_attempts_{clientIp}";
            
            var activity = SuspiciousActivities.GetOrAdd(activityKey, 
                _ => new SuspiciousActivity { IpAddress = clientIp });

            activity.IncrementAuthAttempts();

            if (activity.AuthAttempts > _options.MaxFailedAuthAttemptsPerHour)
            {
                await HandleSecurityEvent(context, SecurityEventType.ExcessiveAuthAttempts,
                    $"來自 {clientIp} 的認證嘗試次數過多: {activity.AuthAttempts}");
                
                // 暫時封鎖IP
                await BlacklistIpTemporarily(clientIp, TimeSpan.FromHours(1));
            }
        }
    }

    private async Task MonitorResponseEvents(HttpContext context, string clientIp)
    {
        var statusCode = context.Response.StatusCode;
        
        if (statusCode == 401 || statusCode == 403)
        {
            await HandleSecurityEvent(context, SecurityEventType.UnauthorizedAccess,
                $"未授權存取嘗試: {context.Request.Method} {context.Request.Path}");
        }
        else if (statusCode == 404)
        {
            // 追蹤404錯誤 - 可能是掃描攻擊
            var activityKey = $"404_errors_{clientIp}";
            var activity = SuspiciousActivities.GetOrAdd(activityKey, 
                _ => new SuspiciousActivity { IpAddress = clientIp });

            activity.Increment404Errors();

            if (activity.NotFoundErrors > _options.Max404ErrorsPerHour)
            {
                await HandleSecurityEvent(context, SecurityEventType.Excessive404Errors,
                    $"來自 {clientIp} 的404錯誤過多: {activity.NotFoundErrors}");
            }
        }
    }

    private async Task HandleSecurityEvent(HttpContext context, SecurityEventType eventType, string details)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            IpAddress = GetClientIpAddress(context),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            RequestPath = context.Request.Path.Value ?? "",
            UserId = GetUserId(context),
            Details = details,
            Severity = GetEventSeverity(eventType)
        };

        // 記錄安全事件
        await LogSecurityEvent(securityEvent);

        // 根據嚴重性採取行動
        await TakeActionBasedOnSeverity(context, securityEvent);
    }

    private async Task BlockRequest(HttpContext context, string reason, SecurityEventType eventType)
    {
        await HandleSecurityEvent(context, eventType, reason);
        
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Access Denied",
            message = "您的請求已被安全系統封鎖",
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task BlacklistIpTemporarily(string ipAddress, TimeSpan duration)
    {
        _cache.Set($"blacklist_ip_{ipAddress}", true, duration);
        
        _logger.LogWarning("IP地址 {IpAddress} 已被暫時封鎖 {Duration} 分鐘", 
            ipAddress, duration.TotalMinutes);
    }

    private static string? GetUserId(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        return null;
    }

    private static SecuritySeverity GetEventSeverity(SecurityEventType eventType)
    {
        return eventType switch
        {
            SecurityEventType.BlockedIpAccess => SecuritySeverity.High,
            SecurityEventType.ExcessiveAuthAttempts => SecuritySeverity.High,
            SecurityEventType.UnauthorizedAccess => SecuritySeverity.Medium,
            SecurityEventType.RateLimitExceeded => SecuritySeverity.Medium,
            SecurityEventType.SuspiciousUserAgent => SecuritySeverity.Low,
            SecurityEventType.AnomalousRequest => SecuritySeverity.Medium,
            SecurityEventType.Excessive404Errors => SecuritySeverity.Low,
            _ => SecuritySeverity.Low
        };
    }

    private async Task LogSecurityEvent(SecurityEvent securityEvent)
    {
        var logLevel = securityEvent.Severity switch
        {
            SecuritySeverity.High => LogLevel.Critical,
            SecuritySeverity.Medium => LogLevel.Warning,
            SecuritySeverity.Low => LogLevel.Information,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel,
            "安全事件 - {EventType} | Severity: {Severity} | IP: {IpAddress} | User: {UserId} | Path: {RequestPath} | Details: {Details}",
            securityEvent.EventType, securityEvent.Severity, securityEvent.IpAddress, 
            securityEvent.UserId ?? "Anonymous", securityEvent.RequestPath, securityEvent.Details);

        // TODO: 在生產環境中，可以將安全事件發送到外部監控系統
        // await SendToSecuritySystem(securityEvent);
    }

    private async Task TakeActionBasedOnSeverity(HttpContext context, SecurityEvent securityEvent)
    {
        switch (securityEvent.Severity)
        {
            case SecuritySeverity.High:
                // 高危險事件 - 立即封鎖IP
                await BlacklistIpTemporarily(securityEvent.IpAddress, TimeSpan.FromHours(4));
                break;
            
            case SecuritySeverity.Medium:
                // 中等危險 - 增加監控
                // 可以實作額外的監控邏輯
                break;
            
            case SecuritySeverity.Low:
                // 低危險 - 僅記錄
                break;
        }
    }
}

/// <summary>
/// 安全監控選項
/// </summary>
public class SecurityMonitoringOptions
{
    public int MaxRequestsPerMinute { get; set; } = 100;
    public int MaxFailedAuthAttemptsPerHour { get; set; } = 20;
    public int Max404ErrorsPerHour { get; set; } = 50;
}

/// <summary>
/// 可疑活動追蹤
/// </summary>
public class SuspiciousActivity
{
    public string IpAddress { get; set; } = "";
    public int AuthAttempts { get; private set; }
    public int NotFoundErrors { get; private set; }
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;

    public void IncrementAuthAttempts()
    {
        AuthAttempts++;
        LastActivity = DateTime.UtcNow;
    }

    public void Increment404Errors()
    {
        NotFoundErrors++;
        LastActivity = DateTime.UtcNow;
    }
}

/// <summary>
/// 安全事件模型
/// </summary>
public class SecurityEvent
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public SecurityEventType EventType { get; set; }
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string RequestPath { get; set; } = "";
    public string? UserId { get; set; }
    public string Details { get; set; } = "";
    public SecuritySeverity Severity { get; set; }
}

/// <summary>
/// 安全事件類型
/// </summary>
public enum SecurityEventType
{
    BlockedIpAccess,
    ExcessiveAuthAttempts,
    UnauthorizedAccess,
    RateLimitExceeded,
    SuspiciousUserAgent,
    AnomalousRequest,
    Excessive404Errors
}

/// <summary>
/// 安全事件嚴重性
/// </summary>
public enum SecuritySeverity
{
    Low,
    Medium,
    High
}

/// <summary>
/// 擴展方法用於註冊安全監控中間件
/// </summary>
public static class SecurityMonitoringMiddlewareExtensions
{
    /// <summary>
    /// 添加安全監控中間件到管道
    /// </summary>
    public static IApplicationBuilder UseSecurityMonitoring(this IApplicationBuilder builder,
        SecurityMonitoringOptions? options = null)
    {
        return builder.UseMiddleware<SecurityMonitoringMiddleware>(options ?? new SecurityMonitoringOptions());
    }
}