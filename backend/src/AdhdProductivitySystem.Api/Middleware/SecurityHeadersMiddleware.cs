using Microsoft.Extensions.Options;

namespace AdhdProductivitySystem.Api.Middleware;

/// <summary>
/// 安全標頭中間件
/// 自動添加和管理 HTTP 安全標頭
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 在響應開始前添加安全標頭
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // 移除可能洩露信息的標頭
            RemoveInformationDisclosureHeaders(response);

            // 添加基礎安全標頭
            AddBasicSecurityHeaders(response);

            // 根據請求類型添加特定標頭
            if (IsApiRequest(request))
            {
                AddApiSecurityHeaders(response);
            }
            else if (IsStaticResourceRequest(request))
            {
                AddStaticResourceHeaders(response);
            }
            else
            {
                AddPageSecurityHeaders(response);
            }

            // 添加 HTTPS 相關標頭（僅在 HTTPS 下）
            if (request.IsHttps || _options.ForceSecureHeaders)
            {
                AddHttpsSecurityHeaders(response);
            }

            // 添加 CSP 標頭
            AddContentSecurityPolicy(request, response);

            // 記錄安全標頭應用情況
            LogSecurityHeadersApplied(request, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "應用安全標頭時發生錯誤");
        }
    }

    /// <summary>
    /// 移除可能洩露信息的標頭
    /// </summary>
    private static void RemoveInformationDisclosureHeaders(HttpResponse response)
    {
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");
        response.Headers.Remove("X-SourceFiles");
    }

    /// <summary>
    /// 添加基礎安全標頭
    /// </summary>
    private void AddBasicSecurityHeaders(HttpResponse response)
    {
        // X-Content-Type-Options
        if (_options.EnableContentTypeOptions)
        {
            response.Headers.Append("X-Content-Type-Options", "nosniff");
        }

        // X-Frame-Options
        if (_options.EnableFrameOptions)
        {
            response.Headers.Append("X-Frame-Options", _options.FrameOptions);
        }

        // X-XSS-Protection
        if (_options.EnableXSSProtection)
        {
            response.Headers.Append("X-XSS-Protection", "1; mode=block");
        }

        // Referrer-Policy
        if (_options.EnableReferrerPolicy)
        {
            response.Headers.Append("Referrer-Policy", _options.ReferrerPolicy);
        }

        // Permissions-Policy
        if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicy))
        {
            response.Headers.Append("Permissions-Policy", _options.PermissionsPolicy);
        }
    }

    /// <summary>
    /// 添加 API 特定的安全標頭
    /// </summary>
    private void AddApiSecurityHeaders(HttpResponse response)
    {
        // API 端點不需要框架保護，但需要更嚴格的快取控制
        response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
        response.Headers.Append("Pragma", "no-cache");
        response.Headers.Append("Expires", "0");

        // API 特定的 CORS 標頭（如果啟用）
        if (_options.EnableCorsHeaders)
        {
            response.Headers.Append("X-Content-Type-Options", "nosniff");
        }
    }

    /// <summary>
    /// 添加靜態資源標頭
    /// </summary>
    private void AddStaticResourceHeaders(HttpResponse response)
    {
        // 靜態資源可以長期快取
        if (_options.EnableStaticResourceCaching)
        {
            response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
        }
    }

    /// <summary>
    /// 添加頁面安全標頭
    /// </summary>
    private void AddPageSecurityHeaders(HttpResponse response)
    {
        // HTML 頁面不快取
        response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate, private");
        response.Headers.Append("Pragma", "no-cache");
        response.Headers.Append("Expires", "0");
    }

    /// <summary>
    /// 添加 HTTPS 相關安全標頭
    /// </summary>
    private void AddHttpsSecurityHeaders(HttpResponse response)
    {
        // HSTS
        if (_options.EnableHSTS)
        {
            var hstsValue = $"max-age={_options.HSTSMaxAge}";
            if (_options.HSTSIncludeSubDomains)
            {
                hstsValue += "; includeSubDomains";
            }
            if (_options.HSTSPreload)
            {
                hstsValue += "; preload";
            }
            response.Headers.Append("Strict-Transport-Security", hstsValue);
        }

        // Expect-CT
        if (_options.EnableExpectCT)
        {
            var expectCtValue = $"max-age={_options.ExpectCTMaxAge}";
            if (_options.ExpectCTEnforce)
            {
                expectCtValue += ", enforce";
            }
            if (!string.IsNullOrEmpty(_options.ExpectCTReportUri))
            {
                expectCtValue += $", report-uri=\"{_options.ExpectCTReportUri}\"";
            }
            response.Headers.Append("Expect-CT", expectCtValue);
        }
    }

    /// <summary>
    /// 添加內容安全政策
    /// </summary>
    private void AddContentSecurityPolicy(HttpRequest request, HttpResponse response)
    {
        if (!_options.EnableCSP)
        {
            return;
        }

        var cspPolicy = GenerateCSPPolicy(request);
        
        if (_options.CSPReportOnly)
        {
            response.Headers.Append("Content-Security-Policy-Report-Only", cspPolicy);
        }
        else
        {
            response.Headers.Append("Content-Security-Policy", cspPolicy);
        }
    }

    /// <summary>
    /// 生成 CSP 政策
    /// </summary>
    private string GenerateCSPPolicy(HttpRequest request)
    {
        var isProduction = _options.IsProduction;
        var isDevelopment = !isProduction;
        var isApiRequest = IsApiRequest(request);

        if (isApiRequest)
        {
            // API 端點的嚴格 CSP
            return "default-src 'none'; frame-ancestors 'none';";
        }

        if (isDevelopment)
        {
            // 開發環境的寬鬆 CSP
            return "default-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                   "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                   "style-src 'self' 'unsafe-inline'; " +
                   "img-src 'self' data: blob:; " +
                   "font-src 'self' data:; " +
                   "connect-src 'self' ws: wss: http: https:; " +
                   "object-src 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self'; " +
                   "frame-ancestors 'none';";
        }

        // 生產環境的嚴格 CSP
        var csp = "default-src 'self'; " +
                  "script-src 'self'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: https:; " +
                  "font-src 'self' data:; " +
                  "connect-src 'self' wss: https:; " +
                  "object-src 'none'; " +
                  "base-uri 'self'; " +
                  "form-action 'self'; " +
                  "frame-ancestors 'none'; " +
                  "upgrade-insecure-requests;";

        // 添加報告端點
        if (!string.IsNullOrEmpty(_options.CSPReportUri))
        {
            csp += $" report-uri {_options.CSPReportUri};";
        }

        return csp;
    }

    /// <summary>
    /// 檢查是否為 API 請求
    /// </summary>
    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api") ||
               request.Headers.Accept.Any(a => a?.Contains("application/json") == true);
    }

    /// <summary>
    /// 檢查是否為靜態資源請求
    /// </summary>
    private static bool IsStaticResourceRequest(HttpRequest request)
    {
        var path = request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf", ".eot" };
        return staticExtensions.Any(ext => path.EndsWith(ext));
    }

    /// <summary>
    /// 記錄安全標頭應用情況
    /// </summary>
    private void LogSecurityHeadersApplied(HttpRequest request, HttpResponse response)
    {
        if (_options.EnableSecurityHeadersLogging)
        {
            var appliedHeaders = response.Headers
                .Where(h => IsSecurityHeader(h.Key))
                .Select(h => h.Key)
                .ToList();

            _logger.LogDebug("已應用安全標頭到 {Path}: {Headers}",
                request.Path, string.Join(", ", appliedHeaders));
        }
    }

    /// <summary>
    /// 檢查是否為安全標頭
    /// </summary>
    private static bool IsSecurityHeader(string headerName)
    {
        var securityHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options",
            "X-XSS-Protection",
            "Referrer-Policy",
            "Content-Security-Policy",
            "Content-Security-Policy-Report-Only",
            "Strict-Transport-Security",
            "Expect-CT",
            "Permissions-Policy"
        };

        return securityHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 安全標頭配置選項
/// </summary>
public class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    // 基礎安全標頭
    public bool EnableContentTypeOptions { get; set; } = true;
    public bool EnableFrameOptions { get; set; } = true;
    public string FrameOptions { get; set; } = "DENY";
    public bool EnableXSSProtection { get; set; } = true;
    public bool EnableReferrerPolicy { get; set; } = true;
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    // Permissions Policy
    public bool EnablePermissionsPolicy { get; set; } = true;
    public string PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=(), magnetometer=(), gyroscope=(), speaker=(), payment=()";

    // HTTPS 相關
    public bool EnableHSTS { get; set; } = true;
    public int HSTSMaxAge { get; set; } = 31536000; // 1年
    public bool HSTSIncludeSubDomains { get; set; } = true;
    public bool HSTSPreload { get; set; } = true;
    public bool ForceSecureHeaders { get; set; } = false;

    public bool EnableExpectCT { get; set; } = true;
    public int ExpectCTMaxAge { get; set; } = 86400; // 1天
    public bool ExpectCTEnforce { get; set; } = true;
    public string? ExpectCTReportUri { get; set; }

    // CSP 相關
    public bool EnableCSP { get; set; } = true;
    public bool CSPReportOnly { get; set; } = false;
    public string? CSPReportUri { get; set; } = "/api/security/csp-report";

    // 其他選項
    public bool EnableCorsHeaders { get; set; } = true;
    public bool EnableStaticResourceCaching { get; set; } = true;
    public bool EnableSecurityHeadersLogging { get; set; } = false;
    public bool IsProduction { get; set; } = true;
}

/// <summary>
/// 安全標頭中間件擴展方法
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// 添加安全標頭中間件
    /// </summary>
    /// <param name="builder">應用程序建構器</param>
    /// <returns>應用程序建構器</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// 註冊安全標頭服務
    /// </summary>
    /// <param name="services">服務集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服務集合</returns>
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(configuration.GetSection(SecurityHeadersOptions.SectionName));
        return services;
    }
}