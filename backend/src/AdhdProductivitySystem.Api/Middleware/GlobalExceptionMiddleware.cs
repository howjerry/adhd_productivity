using AdhdProductivitySystem.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// 全域例外處理中間件
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];
        
        // 記錄詳細錯誤資訊
        _logger.LogError(exception, 
            "全域例外處理 - ErrorId: {ErrorId}, Path: {Path}, Method: {Method}, User: {UserId}", 
            errorId,
            context.Request.Path,
            context.Request.Method,
            context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous");

        // 設定回應內容類型
        context.Response.ContentType = "application/json";

        // 根據例外類型設定狀態碼和錯誤回應
        var (statusCode, response) = CreateErrorResponse(exception, errorId);
        context.Response.StatusCode = (int)statusCode;

        // 序列化並返回錯誤回應
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private (HttpStatusCode statusCode, ApiResponse response) CreateErrorResponse(Exception exception, string errorId)
    {
        return exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.CreateError(
                    "ValidationError",
                    "輸入資料驗證失敗",
                    errorId,
                    _environment.IsDevelopment() ? validationEx.Message : null
                )
            ),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.CreateError(
                    "InvalidArgument",
                    "請求參數無效",
                    errorId,
                    _environment.IsDevelopment() ? argEx.Message : null
                )
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ApiResponse.CreateError(
                    "Unauthorized",
                    "需要有效的身分驗證",
                    errorId
                )
            ),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse.CreateError(
                    "NotFound",
                    "找不到請求的資源",
                    errorId
                )
            ),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.Conflict,
                ApiResponse.CreateError(
                    "InvalidOperation",
                    "操作無效或衝突",
                    errorId,
                    _environment.IsDevelopment() ? invalidOpEx.Message : null
                )
            ),

            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                ApiResponse.CreateError(
                    "Timeout",
                    "請求處理時間過長，請稍後再試",
                    errorId
                )
            ),

            NotSupportedException => (
                HttpStatusCode.NotImplemented,
                ApiResponse.CreateError(
                    "NotSupported",
                    "不支援的操作",
                    errorId
                )
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                CreateInternalServerErrorResponse(exception, errorId)
            )
        };
    }

    private ApiResponse CreateInternalServerErrorResponse(Exception exception, string errorId)
    {
        if (_environment.IsDevelopment())
        {
            return ApiResponse.CreateError(
                "InternalServerError",
                "伺服器內部錯誤",
                errorId,
                new
                {
                    message = exception.Message,
                    stackTrace = exception.StackTrace,
                    innerException = exception.InnerException?.Message
                }
            );
        }

        return ApiResponse.CreateError(
            "InternalServerError",
            "系統發生內部錯誤，請稍後再試",
            errorId
        );
    }
}

/// <summary>
/// 全域例外處理中間件擴展方法
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// 使用全域例外處理中間件
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}