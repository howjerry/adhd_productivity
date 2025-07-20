using System.Diagnostics;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// 效能監控中間件，記錄 API 請求的回應時間和效能指標
/// </summary>
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;
    
    // 效能閾值設定（毫秒）
    private const int SlowRequestThreshold = 1000; // 1 秒
    private const int VerySlowRequestThreshold = 5000; // 5 秒

    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userIP = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // 記錄請求開始
        _logger.LogDebug("開始處理請求: {Method} {Path} from {UserIP}", 
            requestMethod, requestPath, userIP);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;
            
            // 根據回應時間記錄不同等級的日誌
            var logLevel = GetLogLevel(elapsedMilliseconds, statusCode);
            
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestPath"] = requestPath,
                ["RequestMethod"] = requestMethod,
                ["StatusCode"] = statusCode,
                ["ElapsedMilliseconds"] = elapsedMilliseconds,
                ["UserIP"] = userIP,
                ["UserAgent"] = userAgent
            });

            _logger.Log(logLevel, 
                "請求完成: {Method} {Path} -> {StatusCode} 在 {ElapsedMs}ms", 
                requestMethod, requestPath, statusCode, elapsedMilliseconds);

            // 如果是慢請求，記錄額外資訊
            if (elapsedMilliseconds > SlowRequestThreshold)
            {
                var queryString = context.Request.QueryString.Value ?? string.Empty;
                _logger.LogWarning(
                    "慢請求偵測: {Method} {Path}{QueryString} 花費了 {ElapsedMs}ms (閾值: {Threshold}ms)",
                    requestMethod, requestPath, queryString, elapsedMilliseconds, SlowRequestThreshold);
                
                // 可以在這裡添加更多監控邏輯，例如發送到監控系統
                await RecordSlowRequest(context, elapsedMilliseconds);
            }

            // 記錄錯誤回應
            if (statusCode >= 400)
            {
                _logger.LogWarning("錯誤回應: {Method} {Path} -> {StatusCode} 在 {ElapsedMs}ms", 
                    requestMethod, requestPath, statusCode, elapsedMilliseconds);
            }

            // 添加效能標頭到回應中（僅開發環境）
            if (context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            {
                context.Response.Headers.Append("X-Response-Time-Ms", elapsedMilliseconds.ToString());
                context.Response.Headers.Append("X-Performance-Category", GetPerformanceCategory(elapsedMilliseconds));
            }
        }
    }

    /// <summary>
    /// 根據回應時間和狀態碼決定日誌等級
    /// </summary>
    private static LogLevel GetLogLevel(long elapsedMilliseconds, int statusCode)
    {
        // 錯誤狀態碼使用 Warning
        if (statusCode >= 500)
            return LogLevel.Error;
        
        if (statusCode >= 400)
            return LogLevel.Warning;

        // 根據回應時間決定日誌等級
        return elapsedMilliseconds switch
        {
            > VerySlowRequestThreshold => LogLevel.Error,
            > SlowRequestThreshold => LogLevel.Warning,
            > 500 => LogLevel.Information,
            _ => LogLevel.Debug
        };
    }

    /// <summary>
    /// 取得效能分類
    /// </summary>
    private static string GetPerformanceCategory(long elapsedMilliseconds)
    {
        return elapsedMilliseconds switch
        {
            > VerySlowRequestThreshold => "very-slow",
            > SlowRequestThreshold => "slow",
            > 500 => "moderate",
            > 100 => "fast",
            _ => "very-fast"
        };
    }

    /// <summary>
    /// 記錄慢請求詳細資訊
    /// </summary>
    private async Task RecordSlowRequest(HttpContext context, long elapsedMilliseconds)
    {
        try
        {
            // 這裡可以實作將慢請求資訊發送到監控系統
            // 例如：Application Insights, Prometheus, 或自訂的監控服務
            
            var slowRequestInfo = new
            {
                Timestamp = DateTime.UtcNow,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                StatusCode = context.Response.StatusCode,
                ElapsedMilliseconds = elapsedMilliseconds,
                UserIP = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                UserId = context.User?.Identity?.Name // 如果可用的話
            };

            // 範例：記錄到結構化日誌
            _logger.LogInformation("慢請求詳細資訊: {@SlowRequestInfo}", slowRequestInfo);
            
            // 這裡可以添加其他監控邏輯
            // await SendToMonitoringService(slowRequestInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄慢請求資訊時發生錯誤");
        }
    }
}

/// <summary>
/// 效能中間件擴展方法
/// </summary>
public static class PerformanceMiddlewareExtensions
{
    /// <summary>
    /// 添加效能監控中間件到請求管道
    /// </summary>
    /// <param name="builder">應用程式建構器</param>
    /// <returns>應用程式建構器</returns>
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMiddleware>();
    }
}