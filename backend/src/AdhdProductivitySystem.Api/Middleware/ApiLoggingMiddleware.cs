using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// 中間件用於記錄API請求和回應
/// </summary>
public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiLoggingMiddleware> _logger;
    private static readonly string[] SensitiveHeaders = { "authorization", "cookie", "x-api-key", "x-auth-token" };
    private static readonly string[] SensitiveFields = { "password", "token", "secret", "key", "credentials" };

    public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 只記錄API請求，忽略靜態檔案和健康檢查
        if (!ShouldLog(context))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = GenerateRequestId();
        var originalBodyStream = context.Response.Body;

        // 準備記錄請求
        var requestLog = await CreateRequestLog(context, requestId);
        
        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // 記錄回應
            var responseLog = await CreateResponseLog(context, requestId, stopwatch.ElapsedMilliseconds, responseBody);
            
            // 複製回應內容到原始串流
            await responseBody.CopyToAsync(originalBodyStream);

            // 根據狀態碼決定日誌等級
            LogApiCall(requestLog, responseLog);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 記錄異常
            _logger.LogError(ex, "API請求處理異常 - RequestId: {RequestId}, 處理時間: {ElapsedMs}ms", 
                requestId, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldLog(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(path))
            return false;

        // 忽略的路徑
        var ignoredPaths = new[]
        {
            "/health",
            "/swagger",
            "/favicon.ico",
            "/robots.txt"
        };

        return !ignoredPaths.Any(ignored => path.StartsWith(ignored)) && 
               path.StartsWith("/api");
    }

    private static string GenerateRequestId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    private async Task<ApiRequestLog> CreateRequestLog(HttpContext context, string requestId)
    {
        var request = context.Request;
        
        var requestLog = new ApiRequestLog
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path.Value ?? "",
            QueryString = request.QueryString.Value ?? "",
            UserAgent = request.Headers.UserAgent.ToString(),
            IpAddress = GetClientIpAddress(context),
            UserId = GetUserId(context),
            Headers = FilterHeaders(request.Headers),
            ContentType = request.ContentType ?? ""
        };

        // 記錄請求內容（僅對特定方法和內容類型）
        if (ShouldLogRequestBody(request))
        {
            requestLog.Body = await ReadAndSanitizeRequestBody(request);
        }

        return requestLog;
    }

    private async Task<ApiResponseLog> CreateResponseLog(HttpContext context, string requestId, 
        long elapsedMs, MemoryStream responseBody)
    {
        var response = context.Response;
        
        var responseLog = new ApiResponseLog
        {
            RequestId = requestId,
            StatusCode = response.StatusCode,
            ElapsedMilliseconds = elapsedMs,
            ContentType = response.ContentType ?? "",
            ContentLength = responseBody.Length,
            Headers = FilterHeaders(response.Headers.ToDictionary(h => h.Key, h => (StringValues)h.Value.ToArray()))
        };

        // 記錄回應內容（僅在錯誤情況下）
        if (ShouldLogResponseBody(response))
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var content = await new StreamReader(responseBody).ReadToEndAsync();
            responseLog.Body = SanitizeContent(content);
            responseBody.Seek(0, SeekOrigin.Begin);
        }

        return responseLog;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // 嘗試從各種標頭獲取真實IP
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            // X-Forwarded-For 可能包含多個IP，取第一個
            ipAddress = ipAddress.Split(',')[0].Trim();
        }

        ipAddress ??= context.Request.Headers["X-Real-IP"].FirstOrDefault();
        ipAddress ??= context.Connection.RemoteIpAddress?.ToString();

        return ipAddress ?? "unknown";
    }

    private static string? GetUserId(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        return null;
    }

    private static Dictionary<string, string> FilterHeaders(IHeaderDictionary headers)
    {
        var filtered = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            var key = header.Key.ToLowerInvariant();
            var value = SensitiveHeaders.Contains(key) ? "[FILTERED]" : string.Join(", ", header.Value);
            filtered[header.Key] = value;
        }
        
        return filtered;
    }

    private static Dictionary<string, string> FilterHeaders(Dictionary<string, StringValues> headers)
    {
        var filtered = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            var key = header.Key.ToLowerInvariant();
            var value = SensitiveHeaders.Contains(key) ? "[FILTERED]" : string.Join(", ", header.Value);
            filtered[header.Key] = value;
        }
        
        return filtered;
    }

    private static bool ShouldLogRequestBody(HttpRequest request)
    {
        if (request.ContentLength > 10 * 1024) // 限制在10KB以內
            return false;

        var method = request.Method.ToUpperInvariant();
        if (method != "POST" && method != "PUT" && method != "PATCH")
            return false;

        var contentType = request.ContentType?.ToLowerInvariant();
        return contentType?.StartsWith("application/json") == true ||
               contentType?.StartsWith("application/xml") == true ||
               contentType?.StartsWith("text/") == true;
    }

    private static bool ShouldLogResponseBody(HttpResponse response)
    {
        // 只在錯誤狀態碼時記錄回應內容
        return response.StatusCode >= 400 && response.ContentLength < 5 * 1024; // 5KB限制
    }

    private async Task<string> ReadAndSanitizeRequestBody(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;

            var content = Encoding.UTF8.GetString(buffer);
            return SanitizeContent(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "無法讀取請求內容");
            return "[無法讀取]";
        }
    }

    private static string SanitizeContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "";

        try
        {
            // 嘗試解析為JSON並過濾敏感欄位
            var jsonDoc = JsonDocument.Parse(content);
            var sanitized = SanitizeJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            // 如果不是JSON，進行簡單的字串過濾
            var result = content;
            foreach (var field in SensitiveFields)
            {
                // 簡單的欄位過濾（可能不完全準確）
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
                result = System.Text.RegularExpressions.Regex.Replace(result, pattern, 
                    $"\"{field}\": \"[FILTERED]\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return result;
        }
    }

    private static object SanitizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => SanitizeJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeJsonElement).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static Dictionary<string, object?> SanitizeJsonObject(JsonElement obj)
    {
        var result = new Dictionary<string, object?>();
        
        foreach (var property in obj.EnumerateObject())
        {
            var key = property.Name.ToLowerInvariant();
            var value = SensitiveFields.Contains(key) ? "[FILTERED]" : SanitizeJsonElement(property.Value);
            result[property.Name] = value;
        }
        
        return result;
    }

    private void LogApiCall(ApiRequestLog request, ApiResponseLog response)
    {
        var logLevel = response.StatusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, 
            "API請求 - {Method} {Path} | Status: {StatusCode} | Time: {ElapsedMs}ms | IP: {IpAddress} | User: {UserId} | RequestId: {RequestId}",
            request.Method, request.Path, response.StatusCode, response.ElapsedMilliseconds,
            request.IpAddress, request.UserId ?? "Anonymous", request.RequestId);

        // 詳細記錄（Debug等級）
        _logger.LogDebug("API請求詳細 - RequestId: {RequestId}\n請求: {@Request}\n回應: {@Response}",
            request.RequestId, request, response);
    }
}

/// <summary>
/// API請求日誌模型
/// </summary>
public class ApiRequestLog
{
    public string RequestId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string QueryString { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string? UserId { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string ContentType { get; set; } = "";
    public string Body { get; set; } = "";
}

/// <summary>
/// API回應日誌模型
/// </summary>
public class ApiResponseLog
{
    public string RequestId { get; set; } = "";
    public int StatusCode { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string ContentType { get; set; } = "";
    public long ContentLength { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
}

/// <summary>
/// 擴展方法用於註冊API日誌中間件
/// </summary>
public static class ApiLoggingMiddlewareExtensions
{
    /// <summary>
    /// 添加API日誌中間件到管道
    /// </summary>
    public static IApplicationBuilder UseApiLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiLoggingMiddleware>();
    }
}