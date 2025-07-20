using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// 資料庫維護和清理服務
/// </summary>
public class DatabaseMaintenanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    public DatabaseMaintenanceService(ApplicationDbContext context, ILogger<DatabaseMaintenanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 執行完整的資料庫維護
    /// </summary>
    public async Task<MaintenanceReport> PerformMaintenanceAsync(MaintenanceOptions options = null)
    {
        options ??= new MaintenanceOptions();
        var report = new MaintenanceReport();
        
        try
        {
            _logger.LogInformation("開始執行資料庫維護...");
            report.StartedAt = DateTime.UtcNow;

            // 清理過期資料
            if (options.CleanupExpiredData)
            {
                await CleanupExpiredDataAsync(report);
            }

            // 清理孤立記錄
            if (options.CleanupOrphanedRecords)
            {
                await CleanupOrphanedRecordsAsync(report);
            }

            // 優化索引
            if (options.OptimizeIndexes)
            {
                await OptimizeIndexesAsync(report);
            }

            // 更新統計資訊
            if (options.UpdateStatistics)
            {
                await UpdateStatisticsAsync(report);
            }

            // 執行 VACUUM
            if (options.PerformVacuum)
            {
                await PerformVacuumAsync(report);
            }

            // 重新建立索引
            if (options.ReindexTables)
            {
                await ReindexTablesAsync(report);
            }

            // 分析表結構
            if (options.AnalyzeTables)
            {
                await AnalyzeTablesAsync(report);
            }

            // 清理日誌
            if (options.CleanupLogs)
            {
                await CleanupLogsAsync(report);
            }

            report.CompletedAt = DateTime.UtcNow;
            report.IsSuccessful = true;

            _logger.LogInformation("資料庫維護完成 - 耗時: {Duration}", 
                report.CompletedAt - report.StartedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料庫維護過程中發生錯誤");
            report.IsSuccessful = false;
            report.ErrorMessage = ex.Message;
        }

        return report;
    }

    /// <summary>
    /// 清理過期資料
    /// </summary>
    private async Task CleanupExpiredDataAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("清理過期資料...");
            var cleanupTasks = new Dictionary<string, int>();

            // 清理過期的 RefreshTokens（已撤銷且過期超過7天）
            var expiredTokens = await _context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""RefreshTokens""
                WHERE ""IsRevoked"" = true 
                AND ""ExpiresAt"" < NOW() - INTERVAL '7 days'
            ");
            cleanupTasks["過期 RefreshTokens"] = expiredTokens;

            // 清理超過90天的已完成任務的 TimerSessions
            var oldTimerSessions = await _context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""TimerSessions"" ts
                WHERE ts.""Status"" = 2  -- Completed
                AND ts.""EndTime"" < NOW() - INTERVAL '90 days'
                AND NOT EXISTS (
                    SELECT 1 FROM ""Tasks"" t 
                    WHERE t.""Id"" = ts.""TaskId"" 
                    AND t.""Status"" != 3  -- 任務未完成
                )
            ");
            cleanupTasks["舊 TimerSessions"] = oldTimerSessions;

            // 清理超過180天的已處理 CaptureItems
            var oldCaptureItems = await _context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""CaptureItems""
                WHERE ""IsProcessed"" = true
                AND ""ProcessedAt"" < NOW() - INTERVAL '180 days'
                AND ""TaskId"" IS NULL  -- 未關聯到任務
            ");
            cleanupTasks["舊 CaptureItems"] = oldCaptureItems;

            // 清理超過365天的 UserProgress 記錄
            var oldUserProgress = await _context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""UserProgress""
                WHERE ""Date"" < NOW() - INTERVAL '365 days'
            ");
            cleanupTasks["舊 UserProgress"] = oldUserProgress;

            // 清理超過30天的過期 TimeBlocks
            var oldTimeBlocks = await _context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""TimeBlocks""
                WHERE ""EndTime"" < NOW() - INTERVAL '30 days'
                AND ""IsRecurring"" = false
                AND ""IsCompleted"" = true
            ");
            cleanupTasks["舊 TimeBlocks"] = oldTimeBlocks;

            var totalCleaned = cleanupTasks.Values.Sum();
            report.OperationResults["清理過期資料"] = $"清理了 {totalCleaned} 筆記錄: " + 
                string.Join(", ", cleanupTasks.Select(kv => $"{kv.Key}: {kv.Value}"));

            _logger.LogInformation("過期資料清理完成 - 總共清理 {Count} 筆記錄", totalCleaned);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理過期資料時發生錯誤");
            report.OperationResults["清理過期資料"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 清理孤立記錄
    /// </summary>
    private async Task CleanupOrphanedRecordsAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("清理孤立記錄...");
            var cleanupTasks = new Dictionary<string, int>();

            // 清理沒有對應 User 的記錄（這應該不會發生，因為有外鍵約束）
            // 但可以檢查並記錄任何異常情況

            // 清理沒有父任務的子任務（如果父任務被刪除）
            var orphanedSubtasks = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""Tasks"" 
                SET ""ParentTaskId"" = NULL
                WHERE ""ParentTaskId"" IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM ""Tasks"" parent 
                    WHERE parent.""Id"" = ""Tasks"".""ParentTaskId""
                )
            ");
            cleanupTasks["孤立子任務"] = orphanedSubtasks;

            // 清理指向不存在任務的 CaptureItems
            var orphanedCaptureItems = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""CaptureItems""
                SET ""TaskId"" = NULL
                WHERE ""TaskId"" IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM ""Tasks"" t 
                    WHERE t.""Id"" = ""CaptureItems"".""TaskId""
                )
            ");
            cleanupTasks["孤立 CaptureItems"] = orphanedCaptureItems;

            // 清理指向不存在任務的 TimeBlocks
            var orphanedTimeBlocks = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""TimeBlocks""
                SET ""TaskId"" = NULL
                WHERE ""TaskId"" IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM ""Tasks"" t 
                    WHERE t.""Id"" = ""TimeBlocks"".""TaskId""
                )
            ");
            cleanupTasks["孤立 TimeBlocks"] = orphanedTimeBlocks;

            // 清理指向不存在任務的 TimerSessions
            var orphanedTimerSessions = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""TimerSessions""
                SET ""TaskId"" = NULL
                WHERE ""TaskId"" IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM ""Tasks"" t 
                    WHERE t.""Id"" = ""TimerSessions"".""TaskId""
                )
            ");
            cleanupTasks["孤立 TimerSessions"] = orphanedTimerSessions;

            var totalCleaned = cleanupTasks.Values.Sum();
            report.OperationResults["清理孤立記錄"] = $"修復了 {totalCleaned} 筆孤立記錄: " + 
                string.Join(", ", cleanupTasks.Select(kv => $"{kv.Key}: {kv.Value}"));

            _logger.LogInformation("孤立記錄清理完成 - 總共修復 {Count} 筆記錄", totalCleaned);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理孤立記錄時發生錯誤");
            report.OperationResults["清理孤立記錄"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 優化索引
    /// </summary>
    private async Task OptimizeIndexesAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("優化索引...");

            // 重新計算索引統計
            await _context.Database.ExecuteSqlRawAsync("ANALYZE;");

            // 檢查索引膨脹情況
            var bloatedIndexes = await _context.Database.SqlQueryRaw<BloatedIndexInfo>(@"
                SELECT 
                    schemaname,
                    tablename,
                    indexname,
                    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
                    CASE 
                        WHEN pg_relation_size(indexrelid) = 0 THEN 0
                        ELSE ROUND((pg_relation_size(indexrelid)::float / 
                               NULLIF(pg_relation_size(indrelid), 0) * 100)::numeric, 2)
                    END as bloat_ratio
                FROM pg_stat_user_indexes
                WHERE schemaname = 'public'
                AND pg_relation_size(indexrelid) > 1024 * 1024  -- 大於1MB的索引
                ORDER BY pg_relation_size(indexrelid) DESC
            ").ToListAsync();

            var optimizedCount = 0;
            foreach (var index in bloatedIndexes)
            {
                if (index.BloatRatio > 20) // 膨脹率超過20%
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync($@"
                            REINDEX INDEX CONCURRENTLY ""{index.IndexName}""
                        ");
                        optimizedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "重建索引 {IndexName} 時發生錯誤", index.IndexName);
                    }
                }
            }

            report.OperationResults["優化索引"] = $"檢查了 {bloatedIndexes.Count} 個索引，重建了 {optimizedCount} 個膨脹索引";
            _logger.LogInformation("索引優化完成 - 重建 {Count} 個索引", optimizedCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "優化索引時發生錯誤");
            report.OperationResults["優化索引"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 更新統計資訊
    /// </summary>
    private async Task UpdateStatisticsAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("更新統計資訊...");

            await _context.Database.ExecuteSqlRawAsync("ANALYZE;");

            report.OperationResults["更新統計資訊"] = "成功更新所有表的統計資訊";
            _logger.LogInformation("統計資訊更新完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新統計資訊時發生錯誤");
            report.OperationResults["更新統計資訊"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 執行 VACUUM
    /// </summary>
    private async Task PerformVacuumAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("執行 VACUUM...");

            // 檢查需要 VACUUM 的表
            var tablesNeedingVacuum = await _context.Database.SqlQueryRaw<TableVacuumInfo>(@"
                SELECT 
                    schemaname,
                    relname as tablename,
                    n_dead_tup as dead_tuples,
                    n_live_tup as live_tuples,
                    CASE 
                        WHEN n_live_tup + n_dead_tup = 0 THEN 0
                        ELSE ROUND((n_dead_tup * 100.0 / (n_live_tup + n_dead_tup))::numeric, 2)
                    END as dead_tuple_ratio
                FROM pg_stat_user_tables
                WHERE schemaname = 'public'
                AND n_dead_tup > 1000  -- 死元組數量大於1000
                ORDER BY dead_tuple_ratio DESC
            ").ToListAsync();

            var vacuumedTables = 0;
            foreach (var table in tablesNeedingVacuum)
            {
                if (table.DeadTupleRatio > 10) // 死元組比例超過10%
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync($@"
                            VACUUM (VERBOSE, ANALYZE) ""{table.TableName}""
                        ");
                        vacuumedTables++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "VACUUM 表 {TableName} 時發生錯誤", table.TableName);
                    }
                }
            }

            report.OperationResults["執行 VACUUM"] = $"檢查了 {tablesNeedingVacuum.Count} 個表，清理了 {vacuumedTables} 個表";
            _logger.LogInformation("VACUUM 完成 - 清理 {Count} 個表", vacuumedTables);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "執行 VACUUM 時發生錯誤");
            report.OperationResults["執行 VACUUM"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 重新建立索引
    /// </summary>
    private async Task ReindexTablesAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("重新建立索引...");

            var mainTables = new[] { "Users", "Tasks", "CaptureItems", "RefreshTokens", "TimerSessions" };
            var reindexedTables = 0;

            foreach (var table in mainTables)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync($@"
                        REINDEX TABLE CONCURRENTLY ""{table}""
                    ");
                    reindexedTables++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "重建表 {TableName} 索引時發生錯誤", table);
                }
            }

            report.OperationResults["重新建立索引"] = $"重建了 {reindexedTables}/{mainTables.Length} 個主要表的索引";
            _logger.LogInformation("索引重建完成 - {Count} 個表", reindexedTables);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "重新建立索引時發生錯誤");
            report.OperationResults["重新建立索引"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 分析表結構
    /// </summary>
    private async Task AnalyzeTablesAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("分析表結構...");

            var tableAnalysis = await _context.Database.SqlQueryRaw<TableAnalysisInfo>(@"
                SELECT 
                    schemaname,
                    relname as tablename,
                    seq_scan,
                    seq_tup_read,
                    idx_scan,
                    idx_tup_fetch,
                    n_tup_ins as tuples_inserted,
                    n_tup_upd as tuples_updated,
                    n_tup_del as tuples_deleted,
                    n_live_tup as live_tuples,
                    pg_size_pretty(pg_relation_size(relid)) as table_size
                FROM pg_stat_user_tables
                WHERE schemaname = 'public'
                ORDER BY pg_relation_size(relid) DESC
            ").ToListAsync();

            // 檢查表的讀取模式
            var tablesWithHighSeqScan = tableAnalysis.Where(t => 
                t.SeqScan > 0 && t.IdxScan > 0 && 
                (t.SeqScan * 100.0 / (t.SeqScan + t.IdxScan)) > 30 // 順序掃描比例超過30%
            ).ToList();

            var analysisResults = new List<string>();
            if (tablesWithHighSeqScan.Any())
            {
                analysisResults.Add($"發現 {tablesWithHighSeqScan.Count} 個表的順序掃描比例較高，可能需要優化索引");
            }

            var largestTables = tableAnalysis.Take(5).ToList();
            analysisResults.Add($"最大的5個表: {string.Join(", ", largestTables.Select(t => $"{t.TableName}({t.TableSize})"))}");

            report.OperationResults["分析表結構"] = string.Join("; ", analysisResults);
            _logger.LogInformation("表結構分析完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析表結構時發生錯誤");
            report.OperationResults["分析表結構"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 清理日誌
    /// </summary>
    private async Task CleanupLogsAsync(MaintenanceReport report)
    {
        try
        {
            _logger.LogInformation("清理日誌...");

            // 清理 PostgreSQL 日誌（如果有權限）
            // 這通常需要在資料庫層面配置日誌輪替

            // 這裡主要是重置統計資訊中的舊資料
            await _context.Database.ExecuteSqlRawAsync("SELECT pg_stat_reset();");

            report.OperationResults["清理日誌"] = "重置了統計資訊";
            _logger.LogInformation("日誌清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理日誌時發生錯誤");
            report.OperationResults["清理日誌"] = $"失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 快速維護（日常使用）
    /// </summary>
    public async Task<MaintenanceReport> PerformQuickMaintenanceAsync()
    {
        var options = new MaintenanceOptions
        {
            CleanupExpiredData = true,
            UpdateStatistics = true,
            CleanupOrphanedRecords = false,
            OptimizeIndexes = false,
            PerformVacuum = false,
            ReindexTables = false,
            AnalyzeTables = false,
            CleanupLogs = false
        };

        return await PerformMaintenanceAsync(options);
    }

    /// <summary>
    /// 深度維護（週末執行）
    /// </summary>
    public async Task<MaintenanceReport> PerformDeepMaintenanceAsync()
    {
        var options = new MaintenanceOptions
        {
            CleanupExpiredData = true,
            CleanupOrphanedRecords = true,
            OptimizeIndexes = true,
            UpdateStatistics = true,
            PerformVacuum = true,
            ReindexTables = true,
            AnalyzeTables = true,
            CleanupLogs = true
        };

        return await PerformMaintenanceAsync(options);
    }
}

// 支援類別
public class MaintenanceOptions
{
    public bool CleanupExpiredData { get; set; } = true;
    public bool CleanupOrphanedRecords { get; set; } = true;
    public bool OptimizeIndexes { get; set; } = true;
    public bool UpdateStatistics { get; set; } = true;
    public bool PerformVacuum { get; set; } = false;
    public bool ReindexTables { get; set; } = false;
    public bool AnalyzeTables { get; set; } = true;
    public bool CleanupLogs { get; set; } = false;
}

public class MaintenanceReport
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool IsSuccessful { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> OperationResults { get; set; } = new();

    public TimeSpan Duration => CompletedAt - StartedAt;

    public override string ToString()
    {
        var report = $"資料庫維護報告\n";
        report += $"執行時間: {StartedAt:yyyy-MM-dd HH:mm:ss} - {CompletedAt:yyyy-MM-dd HH:mm:ss}\n";
        report += $"總耗時: {Duration}\n";
        report += $"執行結果: {(IsSuccessful ? "成功" : "失敗")}\n";
        
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            report += $"錯誤訊息: {ErrorMessage}\n";
        }
        
        if (OperationResults.Any())
        {
            report += "\n操作結果:\n";
            foreach (var result in OperationResults)
            {
                report += $"- {result.Key}: {result.Value}\n";
            }
        }
        
        return report;
    }
}

// 資料模型
public class BloatedIndexInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string IndexSize { get; set; } = string.Empty;
    public decimal BloatRatio { get; set; }
}

public class TableVacuumInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long DeadTuples { get; set; }
    public long LiveTuples { get; set; }
    public decimal DeadTupleRatio { get; set; }
}

public class TableAnalysisInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long SeqScan { get; set; }
    public long SeqTupRead { get; set; }
    public long IdxScan { get; set; }
    public long IdxTupFetch { get; set; }
    public long TuplesInserted { get; set; }
    public long TuplesUpdated { get; set; }
    public long TuplesDeleted { get; set; }
    public long LiveTuples { get; set; }
    public string TableSize { get; set; } = string.Empty;
}