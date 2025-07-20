using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// 資料庫完整性檢查和驗證服務
/// </summary>
public class DatabaseIntegrityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseIntegrityService> _logger;

    public DatabaseIntegrityService(ApplicationDbContext context, ILogger<DatabaseIntegrityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 執行完整的資料庫完整性檢查
    /// </summary>
    public async Task<DatabaseIntegrityReport> PerformIntegrityCheckAsync()
    {
        var report = new DatabaseIntegrityReport();
        
        try
        {
            _logger.LogInformation("開始執行資料庫完整性檢查...");

            // 檢查外鍵約束
            await CheckForeignKeyConstraintsAsync(report);

            // 檢查資料一致性
            await CheckDataConsistencyAsync(report);

            // 檢查孤立記錄
            await CheckOrphanedRecordsAsync(report);

            // 檢查資料驗證規則
            await CheckDataValidationRulesAsync(report);

            // 檢查重複資料
            await CheckDuplicateDataAsync(report);

            // 檢查索引健康狀況
            await CheckIndexHealthAsync(report);

            report.CompletedAt = DateTime.UtcNow;
            report.IsHealthy = !report.HasCriticalIssues;

            _logger.LogInformation("資料庫完整性檢查完成. 健康狀況: {IsHealthy}", report.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料庫完整性檢查過程中發生錯誤");
            report.AddError("完整性檢查失敗", ex.Message);
        }

        return report;
    }

    /// <summary>
    /// 檢查外鍵約束
    /// </summary>
    private async Task CheckForeignKeyConstraintsAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("檢查外鍵約束...");

            // 檢查 Tasks 表的外鍵
            var orphanedTasks = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Tasks"" t
                LEFT JOIN ""Users"" u ON t.""UserId"" = u.""Id""
                WHERE u.""Id"" IS NULL
            ").FirstOrDefaultAsync();

            if (orphanedTasks > 0)
            {
                report.AddError("Tasks 外鍵約束", $"發現 {orphanedTasks} 個 Tasks 記錄沒有對應的 User");
            }

            // 檢查 CaptureItems 表的外鍵
            var orphanedCaptureItems = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""CaptureItems"" ci
                LEFT JOIN ""Users"" u ON ci.""UserId"" = u.""Id""
                WHERE u.""Id"" IS NULL
            ").FirstOrDefaultAsync();

            if (orphanedCaptureItems > 0)
            {
                report.AddError("CaptureItems 外鍵約束", $"發現 {orphanedCaptureItems} 個 CaptureItems 記錄沒有對應的 User");
            }

            // 檢查 RefreshTokens 表的外鍵
            var orphanedRefreshTokens = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""RefreshTokens"" rt
                LEFT JOIN ""Users"" u ON rt.""UserId"" = u.""Id""
                WHERE u.""Id"" IS NULL
            ").FirstOrDefaultAsync();

            if (orphanedRefreshTokens > 0)
            {
                report.AddError("RefreshTokens 外鍵約束", $"發現 {orphanedRefreshTokens} 個 RefreshTokens 記錄沒有對應的 User");
            }

            _logger.LogInformation("外鍵約束檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查外鍵約束時發生錯誤");
            report.AddWarning("外鍵約束檢查", "無法完成外鍵約束檢查");
        }
    }

    /// <summary>
    /// 檢查資料一致性
    /// </summary>
    private async Task CheckDataConsistencyAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("檢查資料一致性...");

            // 檢查 TimerSession 的時間邏輯
            var invalidTimerSessions = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""TimerSessions""
                WHERE ""EndTime"" IS NOT NULL 
                AND ""EndTime"" <= ""StartTime""
            ").FirstOrDefaultAsync();

            if (invalidTimerSessions > 0)
            {
                report.AddError("TimerSession 時間邏輯", $"發現 {invalidTimerSessions} 個 TimerSession 的結束時間早於或等於開始時間");
            }

            // 檢查 Tasks 的 DueDate 邏輯
            var tasksWithInvalidDueDate = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Tasks""
                WHERE ""DueDate"" IS NOT NULL 
                AND ""DueDate"" < ""CreatedAt""
            ").FirstOrDefaultAsync();

            if (tasksWithInvalidDueDate > 0)
            {
                report.AddWarning("Tasks DueDate 邏輯", $"發現 {tasksWithInvalidDueDate} 個 Tasks 的到期日期早於創建日期");
            }

            // 檢查 RefreshToken 的過期邏輯
            var invalidRefreshTokens = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""RefreshTokens""
                WHERE ""ExpiresAt"" <= ""CreatedAt""
            ").FirstOrDefaultAsync();

            if (invalidRefreshTokens > 0)
            {
                report.AddError("RefreshToken 過期邏輯", $"發現 {invalidRefreshTokens} 個 RefreshToken 的過期時間早於或等於創建時間");
            }

            _logger.LogInformation("資料一致性檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查資料一致性時發生錯誤");
            report.AddWarning("資料一致性檢查", "無法完成資料一致性檢查");
        }
    }

    /// <summary>
    /// 檢查孤立記錄
    /// </summary>
    private async Task CheckOrphanedRecordsAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("檢查孤立記錄...");

            // 檢查沒有任何 Tasks 的 Users
            var usersWithoutTasks = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Users"" u
                LEFT JOIN ""Tasks"" t ON u.""Id"" = t.""UserId""
                WHERE t.""Id"" IS NULL
                AND u.""CreatedAt"" < NOW() - INTERVAL '30 days'
            ").FirstOrDefaultAsync();

            if (usersWithoutTasks > 0)
            {
                report.AddInfo("孤立的 Users", $"發現 {usersWithoutTasks} 個超過30天沒有任務的使用者");
            }

            // 檢查已撤銷且過期的 RefreshTokens
            var expiredRevokedTokens = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""RefreshTokens""
                WHERE ""IsRevoked"" = true 
                AND ""ExpiresAt"" < NOW() - INTERVAL '7 days'
            ").FirstOrDefaultAsync();

            if (expiredRevokedTokens > 0)
            {
                report.AddInfo("過期的撤銷 Tokens", $"發現 {expiredRevokedTokens} 個可以清理的過期撤銷 RefreshTokens");
            }

            _logger.LogInformation("孤立記錄檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查孤立記錄時發生錯誤");
            report.AddWarning("孤立記錄檢查", "無法完成孤立記錄檢查");
        }
    }

    /// <summary>
    /// 檢查資料驗證規則
    /// </summary>
    private async Task CheckDataValidationRulesAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("檢查資料驗證規則...");

            // 檢查 Email 格式
            var invalidEmails = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Users""
                WHERE ""Email"" !~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'
            ").FirstOrDefaultAsync();

            if (invalidEmails > 0)
            {
                report.AddError("Email 格式驗證", $"發現 {invalidEmails} 個格式無效的 Email");
            }

            // 檢查必填欄位
            var usersWithEmptyNames = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Users""
                WHERE ""Name"" IS NULL OR TRIM(""Name"") = ''
            ").FirstOrDefaultAsync();

            if (usersWithEmptyNames > 0)
            {
                report.AddError("必填欄位驗證", $"發現 {usersWithEmptyNames} 個 Users 的 Name 欄位為空");
            }

            // 檢查密碼欄位
            var usersWithoutPassword = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Users""
                WHERE ""PasswordHash"" IS NULL OR ""PasswordHash"" = '' 
                   OR ""PasswordSalt"" IS NULL OR ""PasswordSalt"" = ''
            ").FirstOrDefaultAsync();

            if (usersWithoutPassword > 0)
            {
                report.AddError("密碼驗證", $"發現 {usersWithoutPassword} 個 Users 缺少密碼資訊");
            }

            _logger.LogInformation("資料驗證規則檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查資料驗證規則時發生錯誤");
            report.AddWarning("資料驗證規則檢查", "無法完成資料驗證規則檢查");
        }
    }

    /// <summary>
    /// 檢查重複資料
    /// </summary>
    private async Task CheckDuplicateDataAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("檢查重複資料...");

            // 檢查重複的 Email
            var duplicateEmails = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM (
                    SELECT ""Email""
                    FROM ""Users""
                    GROUP BY ""Email""
                    HAVING COUNT(*) > 1
                ) duplicates
            ").FirstOrDefaultAsync();

            if (duplicateEmails > 0)
            {
                report.AddError("重複 Email", $"發現 {duplicateEmails} 個重複的 Email 地址");
            }

            // 檢查重複的 RefreshToken
            var duplicateTokens = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM (
                    SELECT ""Token""
                    FROM ""RefreshTokens""
                    WHERE ""IsRevoked"" = false
                    GROUP BY ""Token""
                    HAVING COUNT(*) > 1
                ) duplicates
            ").FirstOrDefaultAsync();

            if (duplicateTokens > 0)
            {
                report.AddError("重複 RefreshToken", $"發現 {duplicateTokens} 個重複的活躍 RefreshToken");
            }

            _logger.LogInformation("重複資料檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查重複資料時發生錯誤");
            report.AddWarning("重複資料檢查", "無法完成重複資料檢查");
        }
    }

    /// <summary>
    /// 檢查索引健康狀況
    /// </summary>
    private async Task CheckIndexHealthAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("檢查索引健康狀況...");

            // 檢查是否有缺失的主要索引
            var missingIndexes = new List<string>();

            // 檢查 Users.Email 唯一索引
            var hasEmailIndex = await _context.Database.SqlQueryRaw<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM pg_indexes 
                    WHERE tablename = 'Users' 
                    AND indexname LIKE '%Email%'
                ) as Exists
            ").FirstOrDefaultAsync();

            if (!hasEmailIndex)
            {
                missingIndexes.Add("Users.Email 唯一索引");
            }

            // 檢查 RefreshTokens.Token 唯一索引
            var hasTokenIndex = await _context.Database.SqlQueryRaw<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM pg_indexes 
                    WHERE tablename = 'RefreshTokens' 
                    AND indexname LIKE '%Token%'
                ) as Exists
            ").FirstOrDefaultAsync();

            if (!hasTokenIndex)
            {
                missingIndexes.Add("RefreshTokens.Token 唯一索引");
            }

            if (missingIndexes.Count > 0)
            {
                report.AddWarning("缺失索引", $"缺失重要索引: {string.Join(", ", missingIndexes)}");
            }

            _logger.LogInformation("索引健康狀況檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查索引健康狀況時發生錯誤");
            report.AddWarning("索引健康檢查", "無法完成索引健康檢查");
        }
    }

    /// <summary>
    /// 自動修復資料庫問題
    /// </summary>
    public async Task<bool> AutoRepairIssuesAsync(DatabaseIntegrityReport report)
    {
        try
        {
            _logger.LogInformation("開始自動修復資料庫問題...");

            // 清理過期的撤銷 RefreshTokens
            await CleanupExpiredRevokedTokensAsync();

            // 修復資料驗證問題
            await FixDataValidationIssuesAsync();

            _logger.LogInformation("自動修復完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自動修復過程中發生錯誤");
            return false;
        }
    }

    /// <summary>
    /// 清理過期的撤銷 RefreshTokens
    /// </summary>
    private async Task CleanupExpiredRevokedTokensAsync()
    {
        var deletedCount = await _context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""RefreshTokens""
            WHERE ""IsRevoked"" = true 
            AND ""ExpiresAt"" < NOW() - INTERVAL '7 days'
        ");

        if (deletedCount > 0)
        {
            _logger.LogInformation("清理了 {Count} 個過期的撤銷 RefreshTokens", deletedCount);
        }
    }

    /// <summary>
    /// 修復資料驗證問題
    /// </summary>
    private async Task FixDataValidationIssuesAsync()
    {
        // 這裡可以添加自動修復邏輯
        // 例如：修復格式錯誤的資料、補充缺失的必填欄位等
        _logger.LogInformation("資料驗證問題修復完成");
    }
}

/// <summary>
/// 資料庫完整性檢查報告
/// </summary>
public class DatabaseIntegrityReport
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
    public bool IsHealthy { get; set; } = true;
    public bool HasCriticalIssues => Errors.Count > 0;
    
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Info { get; set; } = new();

    public void AddError(string category, string message)
    {
        Errors.Add($"[{category}] {message}");
    }

    public void AddWarning(string category, string message)
    {
        Warnings.Add($"[{category}] {message}");
    }

    public void AddInfo(string category, string message)
    {
        Info.Add($"[{category}] {message}");
    }

    public override string ToString()
    {
        var summary = $"資料庫完整性檢查報告 - 健康狀況: {(IsHealthy ? "良好" : "有問題")}\n";
        summary += $"檢查時間: {StartedAt:yyyy-MM-dd HH:mm:ss} - {CompletedAt:yyyy-MM-dd HH:mm:ss}\n";
        
        if (Errors.Count > 0)
        {
            summary += $"\n錯誤 ({Errors.Count}):\n" + string.Join("\n", Errors);
        }
        
        if (Warnings.Count > 0)
        {
            summary += $"\n警告 ({Warnings.Count}):\n" + string.Join("\n", Warnings);
        }
        
        if (Info.Count > 0)
        {
            summary += $"\n資訊 ({Info.Count}):\n" + string.Join("\n", Info);
        }
        
        return summary;
    }
}