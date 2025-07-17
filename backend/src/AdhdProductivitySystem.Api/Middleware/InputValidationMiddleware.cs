using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// Middleware for comprehensive input validation and sanitization
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;

    public InputValidationMiddleware(RequestDelegate next, ILogger<InputValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Validate request headers
            if (!ValidateHeaders(context))
            {
                await WriteErrorResponse(context, 400, "Invalid request headers");
                return;
            }

            // Validate request size
            if (!ValidateRequestSize(context))
            {
                await WriteErrorResponse(context, 413, "Request size exceeds maximum allowed");
                return;
            }

            // Validate content type for POST/PUT requests
            if (!ValidateContentType(context))
            {
                await WriteErrorResponse(context, 415, "Unsupported media type");
                return;
            }

            // Validate query parameters
            if (!ValidateQueryParameters(context))
            {
                await WriteErrorResponse(context, 400, "Invalid query parameters");
                return;
            }

            // Check for potential injection attempts
            if (DetectInjectionAttempts(context))
            {
                _logger.LogWarning("Potential injection attempt detected from {RemoteIpAddress}: {RequestPath}",
                    context.Connection.RemoteIpAddress, context.Request.Path);
                await WriteErrorResponse(context, 400, "Invalid request format");
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in input validation middleware");
            await WriteErrorResponse(context, 500, "Internal server error");
        }
    }

    private static bool ValidateHeaders(HttpContext context)
    {
        var request = context.Request;

        // Check for required headers in API requests
        if (request.Path.StartsWithSegments("/api"))
        {
            // Validate User-Agent header exists and is reasonable
            var userAgent = request.Headers.UserAgent.ToString();
            if (string.IsNullOrEmpty(userAgent) || userAgent.Length > 1000)
            {
                return false;
            }

            // Check for suspicious headers
            var suspiciousHeaders = new[] { "X-Forwarded-For", "X-Real-IP", "X-Originating-IP" };
            foreach (var header in suspiciousHeaders)
            {
                if (request.Headers.ContainsKey(header))
                {
                    var value = request.Headers[header].ToString();
                    if (ContainsSuspiciousContent(value))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool ValidateRequestSize(HttpContext context)
    {
        const long maxRequestSize = 10 * 1024 * 1024; // 10MB

        if (context.Request.ContentLength.HasValue)
        {
            return context.Request.ContentLength.Value <= maxRequestSize;
        }

        return true;
    }

    private static bool ValidateContentType(HttpContext context)
    {
        var request = context.Request;

        if (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
        {
            var contentType = request.ContentType?.ToLowerInvariant();

            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            var allowedContentTypes = new[]
            {
                "application/json",
                "application/x-www-form-urlencoded",
                "multipart/form-data",
                "text/plain"
            };

            return allowedContentTypes.Any(allowed => contentType.StartsWith(allowed));
        }

        return true;
    }

    private static bool ValidateQueryParameters(HttpContext context)
    {
        var query = context.Request.Query;

        foreach (var parameter in query)
        {
            // Check parameter name
            if (string.IsNullOrEmpty(parameter.Key) || parameter.Key.Length > 100)
            {
                return false;
            }

            // Check for suspicious parameter names
            if (ContainsSuspiciousContent(parameter.Key))
            {
                return false;
            }

            // Check parameter values
            foreach (var value in parameter.Value)
            {
                if (value != null)
                {
                    // Check value length
                    if (value.Length > 1000)
                    {
                        return false;
                    }

                    // Check for suspicious content
                    if (ContainsSuspiciousContent(value))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool DetectInjectionAttempts(HttpContext context)
    {
        var request = context.Request;

        // Check URL path
        if (ContainsSuspiciousContent(request.Path.Value))
        {
            return true;
        }

        // Check query string
        if (ContainsSuspiciousContent(request.QueryString.Value))
        {
            return true;
        }

        return false;
    }

    private static bool ContainsSuspiciousContent(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        var lowerInput = input.ToLowerInvariant();

        // SQL injection patterns
        var sqlPatterns = new[]
        {
            "union select", "or 1=1", "and 1=1", "drop table", "delete from",
            "insert into", "update set", "alter table", "create table",
            "exec(", "execute(", "sp_", "xp_", "sys.", "information_schema",
            "'; --", "' or '", "\" or \"", "/*", "*/", "--", "xtype=char"
        };

        // XSS patterns
        var xssPatterns = new[]
        {
            "<script", "</script>", "javascript:", "onload=", "onerror=",
            "onmouseover=", "onclick=", "onfocus=", "onblur=", "onchange=",
            "onsubmit=", "eval(", "alert(", "confirm(", "prompt(",
            "document.cookie", "document.write", "window.location",
            "fromcharcode", "unescape(", "string.fromcharcode"
        };

        // Path traversal patterns
        var pathTraversalPatterns = new[]
        {
            "../", "..\\", "%2e%2e", "%2f", "%5c", "....//", "....\\\\",
            "%252e%252e", "%c0%af", "%c1%9c"
        };

        // LDAP injection patterns
        var ldapPatterns = new[]
        {
            "(|(", ")(&", "*)(&", "*))(&", "(cn=*)(&", "(!(cn=",
            ")(cn=*))", ")((", "))(", "))|(", "))(|(", "*))(|(", "*))|("
        };

        // Command injection patterns
        var commandPatterns = new[]
        {
            "|", "&", ";", "$", "`", "&&", "||", "$(", "${", "cat ", "ls ",
            "ps ", "id ", "pwd", "whoami", "uname", "echo ", "curl ", "wget ",
            "nc ", "netcat", "chmod", "chown", "rm -", "killall"
        };

        var allPatterns = sqlPatterns.Concat(xssPatterns).Concat(pathTraversalPatterns)
                                   .Concat(ldapPatterns).Concat(commandPatterns);

        return allPatterns.Any(pattern => lowerInput.Contains(pattern));
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message,
            statusCode,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension methods for registering input validation middleware
/// </summary>
public static class InputValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds input validation middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseInputValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<InputValidationMiddleware>();
    }
}