using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace AdhdProductivitySystem.Api.Controllers;

/// <summary>
/// 安全相關 API 控制器
/// 處理 CSP 違規報告、安全監控等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class SecurityController : ControllerBase
{
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(ILogger<SecurityController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 接收 CSP 違規報告
    /// </summary>
    /// <param name="report">CSP 違規報告內容</param>
    /// <returns>處理結果</returns>
    [HttpPost("csp-report")]
    [Consumes("application/csp-report", "application/reports+json")]
    public async Task<IActionResult> HandleCspReport([FromBody] object report)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();
            var timestamp = DateTime.UtcNow;

            // 記錄 CSP 違規
            _logger.LogWarning("CSP 違規報告 - IP: {ClientIp}, UserAgent: {UserAgent}, 時間: {Timestamp}, 報告: {Report}",
                clientIp, userAgent, timestamp, JsonSerializer.Serialize(report));

            // 解析報告內容
            var reportJson = JsonSerializer.Serialize(report);
            var cspViolation = JsonSerializer.Deserialize<CspViolationReport>(reportJson);

            if (cspViolation?.CspReport != null)
            {
                await ProcessCspViolation(cspViolation.CspReport, clientIp, userAgent);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理 CSP 違規報告時發生錯誤");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// 安全健康檢查端點
    /// </summary>
    /// <returns>系統安全狀態</returns>
    [HttpGet("health")]
    public IActionResult SecurityHealth()
    {
        try
        {
            var securityStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                SecurityFeatures = new
                {
                    CspEnabled = true,
                    HstsEnabled = true,
                    XssProtectionEnabled = true,
                    FrameOptionsEnabled = true,
                    ContentTypeOptionsEnabled = true
                },
                LastCspViolation = GetLastCspViolationTime(),
                ActiveSecurityRules = GetActiveSecurityRulesCount()
            };

            return Ok(securityStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取安全健康狀態時發生錯誤");
            return StatusCode(500, new { Status = "Error", Message = "無法獲取安全狀態" });
        }
    }

    /// <summary>
    /// 獲取安全統計信息（需要管理員權限）
    /// </summary>
    /// <returns>安全統計數據</returns>
    [HttpGet("statistics")]
    // [Authorize(Roles = "Admin")] // 取消註釋以啟用管理員權限檢查
    public IActionResult GetSecurityStatistics()
    {
        try
        {
            var statistics = new
            {
                CspViolations = new
                {
                    TotalToday = GetCspViolationsCount(TimeSpan.FromDays(1)),
                    TotalThisWeek = GetCspViolationsCount(TimeSpan.FromDays(7)),
                    TotalThisMonth = GetCspViolationsCount(TimeSpan.FromDays(30))
                },
                TopViolatingDomains = GetTopViolatingDomains(),
                SecurityHeaders = new
                {
                    ComplianceScore = CalculateSecurityHeadersComplianceScore(),
                    MissingHeaders = GetMissingSecurityHeaders()
                },
                RecommendedActions = GetSecurityRecommendations()
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取安全統計信息時發生錯誤");
            return StatusCode(500, new { Message = "無法獲取安全統計信息" });
        }
    }

    /// <summary>
    /// 安全配置驗證端點
    /// </summary>
    /// <returns>安全配置驗證結果</returns>
    [HttpGet("validate-config")]
    public IActionResult ValidateSecurityConfig()
    {
        try
        {
            var validationResults = new List<SecurityValidationResult>();

            // 檢查關鍵安全標頭
            validationResults.AddRange(ValidateSecurityHeaders());

            // 檢查 CSP 配置
            validationResults.AddRange(ValidateCspConfiguration());

            // 檢查 HTTPS 配置
            validationResults.AddRange(ValidateHttpsConfiguration());

            var overallScore = CalculateOverallSecurityScore(validationResults);

            return Ok(new
            {
                OverallScore = overallScore,
                Status = overallScore >= 80 ? "Good" : overallScore >= 60 ? "Fair" : "Poor",
                ValidationResults = validationResults,
                Recommendations = GenerateSecurityRecommendations(validationResults)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "驗證安全配置時發生錯誤");
            return StatusCode(500, new { Message = "無法驗證安全配置" });
        }
    }

    #region 私有方法

    /// <summary>
    /// 獲取客戶端 IP 地址
    /// </summary>
    /// <returns>客戶端 IP 地址</returns>
    private string GetClientIpAddress()
    {
        var forwardedHeader = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            return forwardedHeader.Split(',')[0].Trim();
        }

        var realIpHeader = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIpHeader))
        {
            return realIpHeader;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// 處理 CSP 違規
    /// </summary>
    /// <param name="violation">違規詳情</param>
    /// <param name="clientIp">客戶端 IP</param>
    /// <param name="userAgent">用戶代理</param>
    private async Task ProcessCspViolation(CspReport violation, string clientIp, string userAgent)
    {
        // 分析違規類型
        var violationType = AnalyzeCspViolationType(violation);
        
        // 檢查是否為已知的誤報
        if (IsKnownFalsePositive(violation))
        {
            _logger.LogInformation("CSP 違規被識別為已知誤報 - URI: {BlockedUri}", violation.BlockedUri);
            return;
        }

        // 檢查是否為潛在攻擊
        if (IsPotentialAttack(violation))
        {
            _logger.LogWarning("檢測到潛在的 CSP 攻擊 - 類型: {ViolationType}, 來源: {ClientIp}, 阻擋 URI: {BlockedUri}",
                violationType, clientIp, violation.BlockedUri);
            
            // 記錄安全事件
            await RecordSecurityEvent(new SecurityEvent
            {
                Type = "CSP_VIOLATION_ATTACK",
                Severity = "High",
                ClientIp = clientIp,
                UserAgent = userAgent,
                Details = $"潛在攻擊 - 類型: {violationType}, 阻擋 URI: {violation.BlockedUri}",
                Timestamp = DateTime.UtcNow
            });
        }

        // 更新 CSP 統計
        await UpdateCspStatistics(violation, violationType);
    }

    /// <summary>
    /// 分析 CSP 違規類型
    /// </summary>
    /// <param name="violation">違規詳情</param>
    /// <returns>違規類型</returns>
    private string AnalyzeCspViolationType(CspReport violation)
    {
        if (violation.ViolatedDirective?.StartsWith("script-src") == true)
        {
            return "Script Injection";
        }
        
        if (violation.ViolatedDirective?.StartsWith("style-src") == true)
        {
            return "Style Injection";
        }
        
        if (violation.ViolatedDirective?.StartsWith("img-src") == true)
        {
            return "Image Source Violation";
        }
        
        if (violation.ViolatedDirective?.StartsWith("connect-src") == true)
        {
            return "Connection Violation";
        }

        return "Unknown Violation";
    }

    /// <summary>
    /// 檢查是否為已知誤報
    /// </summary>
    /// <param name="violation">違規詳情</param>
    /// <returns>是否為誤報</returns>
    private bool IsKnownFalsePositive(CspReport violation)
    {
        var knownFalsePositives = new[]
        {
            "chrome-extension://",
            "moz-extension://",
            "safari-extension://",
            "about:blank",
            "data:image/",
            "blob:"
        };

        return knownFalsePositives.Any(fp => violation.BlockedUri?.StartsWith(fp) == true);
    }

    /// <summary>
    /// 檢查是否為潛在攻擊
    /// </summary>
    /// <param name="violation">違規詳情</param>
    /// <returns>是否為潛在攻擊</returns>
    private bool IsPotentialAttack(CspReport violation)
    {
        var attackPatterns = new[]
        {
            "javascript:",
            "vbscript:",
            "data:text/html",
            "eval(",
            "alert(",
            "document.write",
            "innerHTML"
        };

        var blockedUri = violation.BlockedUri ?? "";
        var sourceFile = violation.SourceFile ?? "";

        return attackPatterns.Any(pattern => 
            blockedUri.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
            sourceFile.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 記錄安全事件
    /// </summary>
    /// <param name="securityEvent">安全事件</param>
    private async Task RecordSecurityEvent(SecurityEvent securityEvent)
    {
        // 這裡可以實現將安全事件存儲到數據庫或發送到安全監控系統
        _logger.LogWarning("安全事件記錄 - 類型: {Type}, 嚴重程度: {Severity}, 客戶端: {ClientIp}",
            securityEvent.Type, securityEvent.Severity, securityEvent.ClientIp);
        
        // 如果是高嚴重性事件，可以觸發額外的警報
        if (securityEvent.Severity == "High")
        {
            // 發送安全警報
            await SendSecurityAlert(securityEvent);
        }
    }

    /// <summary>
    /// 發送安全警報
    /// </summary>
    /// <param name="securityEvent">安全事件</param>
    private async Task SendSecurityAlert(SecurityEvent securityEvent)
    {
        // 實現安全警報邏輯，例如發送郵件、Slack 通知等
        _logger.LogCritical("發送安全警報 - 事件: {EventType}, 詳情: {Details}",
            securityEvent.Type, securityEvent.Details);
    }

    /// <summary>
    /// 更新 CSP 統計
    /// </summary>
    /// <param name="violation">違規詳情</param>
    /// <param name="violationType">違規類型</param>
    private async Task UpdateCspStatistics(CspReport violation, string violationType)
    {
        // 這裡可以實現 CSP 統計的更新邏輯
        // 例如存儲到緩存或數據庫中
        _logger.LogInformation("更新 CSP 統計 - 類型: {ViolationType}, 指令: {Directive}",
            violationType, violation.ViolatedDirective);
    }

    // 其他統計和驗證方法的實現...
    private DateTime? GetLastCspViolationTime() => DateTime.UtcNow.AddHours(-2); // 示例實現
    private int GetActiveSecurityRulesCount() => 25; // 示例實現
    private int GetCspViolationsCount(TimeSpan timeSpan) => 0; // 示例實現
    private List<string> GetTopViolatingDomains() => new(); // 示例實現
    private int CalculateSecurityHeadersComplianceScore() => 85; // 示例實現
    private List<string> GetMissingSecurityHeaders() => new(); // 示例實現
    private List<string> GetSecurityRecommendations() => new(); // 示例實現
    
    private List<SecurityValidationResult> ValidateSecurityHeaders() => new(); // 示例實現
    private List<SecurityValidationResult> ValidateCspConfiguration() => new(); // 示例實現
    private List<SecurityValidationResult> ValidateHttpsConfiguration() => new(); // 示例實現
    private int CalculateOverallSecurityScore(List<SecurityValidationResult> results) => 80; // 示例實現
    private List<string> GenerateSecurityRecommendations(List<SecurityValidationResult> results) => new(); // 示例實現

    #endregion
}

#region 數據模型

/// <summary>
/// CSP 違規報告模型
/// </summary>
public class CspViolationReport
{
    public CspReport? CspReport { get; set; }
}

/// <summary>
/// CSP 報告詳情
/// </summary>
public class CspReport
{
    public string? DocumentUri { get; set; }
    public string? Referrer { get; set; }
    public string? ViolatedDirective { get; set; }
    public string? EffectiveDirective { get; set; }
    public string? OriginalPolicy { get; set; }
    public string? BlockedUri { get; set; }
    public string? SourceFile { get; set; }
    public int? LineNumber { get; set; }
    public int? ColumnNumber { get; set; }
    public string? Sample { get; set; }
}

/// <summary>
/// 安全事件模型
/// </summary>
public class SecurityEvent
{
    public required string Type { get; set; }
    public required string Severity { get; set; }
    public required string ClientIp { get; set; }
    public required string UserAgent { get; set; }
    public required string Details { get; set; }
    public required DateTime Timestamp { get; set; }
}

/// <summary>
/// 安全驗證結果
/// </summary>
public class SecurityValidationResult
{
    public required string Category { get; set; }
    public required string Check { get; set; }
    public required bool Passed { get; set; }
    public required string Message { get; set; }
    public required int Score { get; set; }
}

#endregion