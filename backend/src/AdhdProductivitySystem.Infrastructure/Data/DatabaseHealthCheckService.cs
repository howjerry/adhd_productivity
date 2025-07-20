using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// 資料庫健康檢查和警報服務
/// </summary>
public class DatabaseHealthCheckService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheckService> _logger;
    private readonly DatabaseHealthCheckOptions _options;

    public DatabaseHealthCheckService(
        ApplicationDbContext context, 
        ILogger<DatabaseHealthCheckService> logger,
        IOptions<DatabaseHealthCheckOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 執行完整的健康檢查
    /// </summary>
    public async Task<HealthCheckResult> PerformHealthCheckAsync()
    {
        var result = new HealthCheckResult
        {
            CheckedAt = DateTime.UtcNow,
            CheckId = Guid.NewGuid().ToString("N")[..8]
        };

        try
        {
            _logger.LogInformation("開始執行資料庫健康檢查 [CheckId: {CheckId}]", result.CheckId);

            // 基本連線檢查
            await CheckDatabaseConnectionAsync(result);

            // 效能指標檢查
            await CheckPerformanceMetricsAsync(result);

            // 資料完整性檢查
            await CheckDataIntegrityAsync(result);

            // 儲存空間檢查
            await CheckStorageSpaceAsync(result);

            // 索引健康檢查
            await CheckIndexHealthAsync(result);

            // 鎖定和阻塞檢查
            await CheckLocksAndBlockingAsync(result);

            // 備份狀態檢查
            await CheckBackupStatusAsync(result);

            // 計算整體健康分數
            CalculateOverallHealth(result);

            // 觸發警報（如果需要）
            await TriggerAlertsIfNeededAsync(result);

            _logger.LogInformation("資料庫健康檢查完成 [CheckId: {CheckId}] - 狀態: {Status}, 分數: {Score}",
                result.CheckId, result.OverallStatus, result.HealthScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "執行資料庫健康檢查時發生錯誤 [CheckId: {CheckId}]", result.CheckId);
            result.OverallStatus = HealthStatus.Critical;
            result.Issues.Add(new HealthIssue
            {
                Severity = IssueSeverity.Critical,
                Category = "系統錯誤",
                Description = $"健康檢查執行失敗: {ex.Message}",
                RecommendedAction = "檢查系統日誌並聯絡系統管理員"
            });
        }

        return result;
    }

    /// <summary>
    /// 檢查資料庫連線
    /// </summary>
    private async Task CheckDatabaseConnectionAsync(HealthCheckResult result)
    {
        try
        {
            // 測試基本連線
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                result.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = "連線",
                    Description = "無法連接到資料庫",
                    RecommendedAction = "檢查資料庫服務狀態和網路連線"
                });
                return;
            }

            // 檢查連線池狀態
            var connectionStats = await _context.Database.SqlQueryRaw<ConnectionStats>(@"
                SELECT 
                    COUNT(*) as total_connections,
                    COUNT(CASE WHEN state = 'active' THEN 1 END) as active_connections,
                    COUNT(CASE WHEN state = 'idle' THEN 1 END) as idle_connections
                FROM pg_stat_activity
                WHERE datname = current_database()
            ").FirstOrDefaultAsync();

            if (connectionStats != null)
            {
                result.Metrics["總連線數"] = connectionStats.TotalConnections.ToString();
                result.Metrics["活躍連線數"] = connectionStats.ActiveConnections.ToString();
                result.Metrics["閒置連線數"] = connectionStats.IdleConnections.ToString();

                // 檢查連線數是否過高
                if (connectionStats.TotalConnections > _options.MaxConnectionsThreshold)
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "連線",
                        Description = $"連線數過高: {connectionStats.TotalConnections}",
                        RecommendedAction = "檢查應用程式連線池配置"
                    });
                }
            }

            _logger.LogDebug("資料庫連線檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查資料庫連線時發生錯誤");
            result.Issues.Add(new HealthIssue
            {
                Severity = IssueSeverity.Critical,
                Category = "連線",
                Description = $"連線檢查失敗: {ex.Message}",
                RecommendedAction = "檢查資料庫連線配置"
            });
        }
    }

    /// <summary>
    /// 檢查效能指標
    /// </summary>
    private async Task CheckPerformanceMetricsAsync(HealthCheckResult result)
    {
        try
        {
            // 檢查緩存命中率
            var cacheStats = await _context.Database.SqlQueryRaw<CacheStats>(@"
                SELECT 
                    SUM(blks_hit) as cache_hits,
                    SUM(blks_read) as disk_reads,
                    CASE 
                        WHEN SUM(blks_hit) + SUM(blks_read) = 0 THEN 0
                        ELSE ROUND(SUM(blks_hit) * 100.0 / (SUM(blks_hit) + SUM(blks_read)), 2)
                    END as cache_hit_ratio
                FROM pg_stat_database
                WHERE datname = current_database()
            ").FirstOrDefaultAsync();

            if (cacheStats != null)
            {
                result.Metrics["緩存命中率"] = $"{cacheStats.CacheHitRatio}%";
                
                if (cacheStats.CacheHitRatio < (decimal)_options.MinCacheHitRatio)
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "效能",
                        Description = $"緩存命中率過低: {cacheStats.CacheHitRatio}%",
                        RecommendedAction = "考慮增加 shared_buffers 或優化查詢"
                    });
                }
            }

            // 檢查慢查詢
            var slowQueryCount = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM pg_stat_statements 
                WHERE mean_time > 1000  -- 大於1秒的查詢
            ").FirstOrDefaultAsync();

            result.Metrics["慢查詢數量"] = slowQueryCount.ToString();
            
            if (slowQueryCount > _options.MaxSlowQueries)
            {
                result.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = "效能",
                    Description = $"發現 {slowQueryCount} 個慢查詢",
                    RecommendedAction = "檢查並優化慢查詢"
                });
            }

            _logger.LogDebug("效能指標檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查效能指標時發生錯誤");
        }
    }

    /// <summary>
    /// 檢查資料完整性
    /// </summary>
    private async Task CheckDataIntegrityAsync(HealthCheckResult result)
    {
        try
        {
            // 檢查主要表的記錄數量
            var userCount = await _context.Users.CountAsync();
            var taskCount = await _context.Tasks.CountAsync();
            var captureItemCount = await _context.CaptureItems.CountAsync();

            result.Metrics["使用者數量"] = userCount.ToString();
            result.Metrics["任務數量"] = taskCount.ToString();
            result.Metrics["捕獲項目數量"] = captureItemCount.ToString();

            // 檢查重要約束
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
                result.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = "資料完整性",
                    Description = $"發現 {duplicateEmails} 個重複的 Email 地址",
                    RecommendedAction = "立即修復重複的 Email 記錄"
                });
            }

            // 檢查孤立記錄
            var orphanedTasks = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM ""Tasks"" t
                LEFT JOIN ""Users"" u ON t.""UserId"" = u.""Id""
                WHERE u.""Id"" IS NULL
            ").FirstOrDefaultAsync();

            if (orphanedTasks > 0)
            {
                result.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = "資料完整性",
                    Description = $"發現 {orphanedTasks} 個孤立的任務記錄",
                    RecommendedAction = "清理孤立記錄或修復外鍵關係"
                });
            }

            _logger.LogDebug("資料完整性檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查資料完整性時發生錯誤");
        }
    }

    /// <summary>
    /// 檢查儲存空間
    /// </summary>
    private async Task CheckStorageSpaceAsync(HealthCheckResult result)
    {
        try
        {
            var databaseSize = await _context.Database.SqlQueryRaw<DatabaseSize>(@"
                SELECT 
                    pg_database_size(current_database()) as size_bytes,
                    pg_size_pretty(pg_database_size(current_database())) as size_pretty
            ").FirstOrDefaultAsync();

            if (databaseSize != null)
            {
                result.Metrics["資料庫大小"] = databaseSize.SizePretty;
                
                var sizeGB = databaseSize.SizeBytes / (1024.0 * 1024.0 * 1024.0);
                if (sizeGB > _options.MaxDatabaseSizeGB)
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "儲存空間",
                        Description = $"資料庫大小過大: {databaseSize.SizePretty}",
                        RecommendedAction = "考慮資料歸檔或清理舊資料"
                    });
                }
            }

            _logger.LogDebug("儲存空間檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查儲存空間時發生錯誤");
        }
    }

    /// <summary>
    /// 檢查索引健康
    /// </summary>
    private async Task CheckIndexHealthAsync(HealthCheckResult result)
    {
        try
        {
            var unusedIndexes = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM pg_stat_user_indexes 
                WHERE idx_scan = 0 
                AND schemaname = 'public'
            ").FirstOrDefaultAsync();

            result.Metrics["未使用索引數量"] = unusedIndexes.ToString();
            
            if (unusedIndexes > _options.MaxUnusedIndexes)
            {
                result.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Info,
                    Category = "索引健康",
                    Description = $"發現 {unusedIndexes} 個未使用的索引",
                    RecommendedAction = "考慮移除未使用的索引以節省空間"
                });
            }

            _logger.LogDebug("索引健康檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查索引健康時發生錯誤");
        }
    }

    /// <summary>
    /// 檢查鎖定和阻塞
    /// </summary>
    private async Task CheckLocksAndBlockingAsync(HealthCheckResult result)
    {
        try
        {
            var blockedQueries = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM pg_stat_activity
                WHERE wait_event_type = 'Lock'
                AND datname = current_database()
            ").FirstOrDefaultAsync();

            result.Metrics["阻塞查詢數量"] = blockedQueries.ToString();
            
            if (blockedQueries > 0)
            {
                result.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = "鎖定",
                    Description = $"發現 {blockedQueries} 個被阻塞的查詢",
                    RecommendedAction = "檢查長時間運行的查詢並考慮終止"
                });
            }

            _logger.LogDebug("鎖定和阻塞檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查鎖定和阻塞時發生錯誤");
        }
    }

    /// <summary>
    /// 檢查備份狀態
    /// </summary>
    private async Task CheckBackupStatusAsync(HealthCheckResult result)
    {
        try
        {
            // 這裡可以檢查備份相關的指標
            // 例如：最後備份時間、WAL 歸檔狀態等
            
            // 檢查 WAL 歸檔狀態（如果啟用）
            var walStatus = await _context.Database.SqlQueryRaw<WalStatus>(@"
                SELECT 
                    CASE WHEN setting = 'on' THEN true ELSE false END as archive_mode,
                    (SELECT COUNT(*) FROM pg_ls_dir('pg_wal') WHERE pg_ls_dir ~ '\.ready$') as ready_wal_files
                FROM pg_settings 
                WHERE name = 'archive_mode'
            ").FirstOrDefaultAsync();

            if (walStatus != null)
            {
                result.Metrics["WAL 歸檔啟用"] = walStatus.ArchiveMode.ToString();
                result.Metrics["待歸檔 WAL 檔案"] = walStatus.ReadyWalFiles.ToString();
                
                if (walStatus.ArchiveMode && walStatus.ReadyWalFiles > _options.MaxReadyWalFiles)
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "備份",
                        Description = $"WAL 檔案堆積: {walStatus.ReadyWalFiles} 個檔案待歸檔",
                        RecommendedAction = "檢查 WAL 歸檔程序"
                    });
                }
            }

            _logger.LogDebug("備份狀態檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "檢查備份狀態時發生錯誤");
        }
    }

    /// <summary>
    /// 計算整體健康分數
    /// </summary>
    private void CalculateOverallHealth(HealthCheckResult result)
    {
        var score = 100;
        var criticalCount = 0;
        var warningCount = 0;

        foreach (var issue in result.Issues)
        {
            switch (issue.Severity)
            {
                case IssueSeverity.Critical:
                    score -= 30;
                    criticalCount++;
                    break;
                case IssueSeverity.Warning:
                    score -= 10;
                    warningCount++;
                    break;
                case IssueSeverity.Info:
                    score -= 2;
                    break;
            }
        }

        result.HealthScore = Math.Max(0, score);

        // 決定整體狀態
        if (criticalCount > 0)
        {
            result.OverallStatus = HealthStatus.Critical;
        }
        else if (warningCount > 3 || result.HealthScore < 70)
        {
            result.OverallStatus = HealthStatus.Warning;
        }
        else if (warningCount > 0 || result.HealthScore < 90)
        {
            result.OverallStatus = HealthStatus.Degraded;
        }
        else
        {
            result.OverallStatus = HealthStatus.Healthy;
        }
    }

    /// <summary>
    /// 觸發警報
    /// </summary>
    private async Task TriggerAlertsIfNeededAsync(HealthCheckResult result)
    {
        try
        {
            if (result.OverallStatus == HealthStatus.Critical)
            {
                _logger.LogError("資料庫健康狀況嚴重 [CheckId: {CheckId}] - 觸發緊急警報", result.CheckId);
                // 這裡可以整合外部警報系統
                // 例如：發送郵件、Slack 通知、簡訊等
            }
            else if (result.OverallStatus == HealthStatus.Warning)
            {
                _logger.LogWarning("資料庫健康狀況警告 [CheckId: {CheckId}] - 觸發警告通知", result.CheckId);
                // 發送警告通知
            }

            // 可以在這裡整合其他警報系統
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "觸發警報時發生錯誤");
        }
    }
}

// 配置選項
public class DatabaseHealthCheckOptions
{
    public int MaxConnectionsThreshold { get; set; } = 80;
    public double MinCacheHitRatio { get; set; } = 95.0;
    public int MaxSlowQueries { get; set; } = 5;
    public double MaxDatabaseSizeGB { get; set; } = 10.0;
    public int MaxUnusedIndexes { get; set; } = 10;
    public int MaxReadyWalFiles { get; set; } = 5;
}

// 健康檢查結果
public class HealthCheckResult
{
    public string CheckId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public HealthStatus OverallStatus { get; set; } = HealthStatus.Healthy;
    public int HealthScore { get; set; } = 100;
    public Dictionary<string, string> Metrics { get; set; } = new();
    public List<HealthIssue> Issues { get; set; } = new();

    public override string ToString()
    {
        var report = $"資料庫健康檢查報告 [CheckId: {CheckId}]\n";
        report += $"檢查時間: {CheckedAt:yyyy-MM-dd HH:mm:ss}\n";
        report += $"整體狀態: {OverallStatus}\n";
        report += $"健康分數: {HealthScore}/100\n\n";

        if (Metrics.Any())
        {
            report += "關鍵指標:\n";
            foreach (var metric in Metrics)
            {
                report += $"- {metric.Key}: {metric.Value}\n";
            }
            report += "\n";
        }

        if (Issues.Any())
        {
            report += "發現的問題:\n";
            foreach (var issue in Issues.OrderBy(i => i.Severity))
            {
                report += $"[{issue.Severity}] {issue.Category}: {issue.Description}\n";
                report += $"  建議行動: {issue.RecommendedAction}\n\n";
            }
        }

        return report;
    }
}

// 枚舉和資料模型
public enum HealthStatus
{
    Healthy,
    Degraded,
    Warning,
    Critical
}

public enum IssueSeverity
{
    Info,
    Warning,
    Critical
}

public class HealthIssue
{
    public IssueSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public class ConnectionStats
{
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
}

public class CacheStats
{
    public long CacheHits { get; set; }
    public long DiskReads { get; set; }
    public decimal CacheHitRatio { get; set; }
}

public class DatabaseSize
{
    public long SizeBytes { get; set; }
    public string SizePretty { get; set; } = string.Empty;
}

public class WalStatus
{
    public bool ArchiveMode { get; set; }
    public int ReadyWalFiles { get; set; }
}