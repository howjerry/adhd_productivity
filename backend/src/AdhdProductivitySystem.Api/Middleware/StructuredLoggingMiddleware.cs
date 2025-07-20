using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// 結構化日誌中間件
/// 為每個 HTTP 請求添加上下文資訊和結構化日誌記錄
/// </summary>
public class StructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StructuredLoggingMiddleware> _logger;

    public StructuredLoggingMiddleware(RequestDelegate next, ILogger<StructuredLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 生成請求 ID
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        // 從 HTTP 標頭取得額外的追蹤資訊
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString("N")[..8];
        
        // 設定日誌上下文
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ClientIP", GetClientIP(context)))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("QueryString", context.Request.QueryString.Value))
        {
            // 添加使用者資訊（如果已認證）
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                using (LogContext.PushProperty("UserId", context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value))
                using (LogContext.PushProperty("UserName", context.User.FindFirst(ClaimTypes.Name)?.Value))
                using (LogContext.PushProperty("UserEmail", context.User.FindFirst(ClaimTypes.Email)?.Value))
                {
                    await ProcessRequestAsync(context, requestId, correlationId);
                }
            }
            else
            {
                await ProcessRequestAsync(context, requestId, correlationId);
            }
        }
    }

    private async Task ProcessRequestAsync(HttpContext context, string requestId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;

        // 記錄請求開始
        _logger.LogInformation("HTTP Request Started: {Method} {Path} {QueryString}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Request.QueryString.Value);

        // 添加回應標頭
        context.Response.Headers.Append("X-Request-ID", requestId);
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        Exception? exception = null;
        var statusCode = 200;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            exception = ex;
            statusCode = 500;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var endTime = DateTimeOffset.UtcNow;

            // 決定日誌級別
            var logLevel = DetermineLogLevel(statusCode, elapsedMs, exception);

            // 建立結構化日誌資料
            var logData = new
            {
                RequestId = requestId,
                CorrelationId = correlationId,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                StatusCode = statusCode,
                ElapsedMs = elapsedMs,
                StartTime = startTime,
                EndTime = endTime,
                ContentLength = context.Response.ContentLength,
                ContentType = context.Response.ContentType,
                ClientIP = GetClientIP(context),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Referer = context.Request.Headers.Referer.ToString(),
                IsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
                UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = context.User?.FindFirst(ClaimTypes.Name)?.Value,
                Exception = exception?.ToString()
            };

            // 記錄請求完成
            using (LogContext.PushProperty("RequestData", logData, destructureObjects: true))
            {
                if (exception != null)
                {
                    _logger.Log(logLevel,
                        exception,
                        "HTTP Request Failed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms - {ExceptionMessage}",
                        context.Request.Method,
                        context.Request.Path.Value,
                        statusCode,
                        elapsedMs,
                        exception.Message);
                }
                else
                {
                    _logger.Log(logLevel,
                        "HTTP Request Completed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path.Value,
                        statusCode,
                        elapsedMs);
                }
            }

            // 記錄效能警告
            if (elapsedMs > 5000) // 超過 5 秒
            {
                _logger.LogWarning("Slow Request Detected: {Method} {Path} took {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path.Value,
                    elapsedMs);
            }

            // 記錄錯誤統計
            if (statusCode >= 400)
            {
                using (LogContext.PushProperty("ErrorCategory", GetErrorCategory(statusCode)))
                {
                    _logger.LogWarning("HTTP Error Response: {Method} {Path} responded {StatusCode}",
                        context.Request.Method,
                        context.Request.Path.Value,
                        statusCode);
                }
            }
        }
    }

    /// <summary>
    /// 取得客戶端 IP 地址
    /// </summary>
    private static string GetClientIP(HttpContext context)
    {
        // 檢查 X-Forwarded-For 標頭（反向代理）
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var firstIP = xForwardedFor.Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(firstIP, out _))
            {
                return firstIP;
            }
        }

        // 檢查 X-Real-IP 標頭（Nginx）
        var xRealIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIP) && System.Net.IPAddress.TryParse(xRealIP, out _))
        {
            return xRealIP;
        }

        // 使用直接連線 IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 根據狀態碼和執行時間決定日誌級別
    /// </summary>
    private static LogLevel DetermineLogLevel(int statusCode, long elapsedMs, Exception? exception)
    {
        if (exception != null)
            return LogLevel.Error;

        if (statusCode >= 500)
            return LogLevel.Error;

        if (statusCode >= 400)
            return LogLevel.Warning;

        if (elapsedMs > 5000) // 超過 5 秒
            return LogLevel.Warning;

        if (elapsedMs > 1000) // 超過 1 秒
            return LogLevel.Information;

        return LogLevel.Debug;
    }

    /// <summary>
    /// 取得錯誤分類
    /// </summary>
    private static string GetErrorCategory(int statusCode)
    {
        return statusCode switch
        {
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            405 => "MethodNotAllowed",
            409 => "Conflict",
            422 => "ValidationError",
            429 => "RateLimit",
            >= 500 => "ServerError",
            >= 400 => "ClientError",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// 結構化日誌中間件擴展方法
/// </summary>
public static class StructuredLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseStructuredLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StructuredLoggingMiddleware>();
    }
}