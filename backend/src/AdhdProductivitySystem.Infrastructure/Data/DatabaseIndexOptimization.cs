using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// 資料庫索引優化服務
/// </summary>
public class DatabaseIndexOptimization
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseIndexOptimization> _logger;

    public DatabaseIndexOptimization(ApplicationDbContext context, ILogger<DatabaseIndexOptimization> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 分析並優化資料庫索引
    /// </summary>
    public async Task AnalyzeAndOptimizeIndexesAsync()
    {
        try
        {
            _logger.LogInformation("開始分析資料庫索引使用情況...");

            // 分析未使用的索引
            await AnalyzeUnusedIndexesAsync();

            // 分析缺失的索引
            await AnalyzeMissingIndexesAsync();

            // 分析索引碎片化
            await AnalyzeIndexFragmentationAsync();

            // 更新統計資訊
            await UpdateStatisticsAsync();

            _logger.LogInformation("資料庫索引分析完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料庫索引分析過程中發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 分析未使用的索引
    /// </summary>
    private async Task AnalyzeUnusedIndexesAsync()
    {
        const string sql = @"
            SELECT 
                schemaname,
                tablename,
                indexname,
                idx_tup_read,
                idx_tup_fetch,
                idx_scan
            FROM pg_stat_user_indexes 
            WHERE idx_scan = 0 
            AND schemaname = 'public'
            ORDER BY tablename, indexname;
        ";

        try
        {
            var unusedIndexes = await _context.Database.SqlQueryRaw<UnusedIndexInfo>(sql).ToListAsync();
            
            if (unusedIndexes.Any())
            {
                _logger.LogWarning("發現 {Count} 個未使用的索引:", unusedIndexes.Count);
                foreach (var index in unusedIndexes)
                {
                    _logger.LogWarning("- {Schema}.{Table}.{Index}", 
                        index.SchemaName, index.TableName, index.IndexName);
                }
            }
            else
            {
                _logger.LogInformation("沒有發現未使用的索引");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析未使用索引時發生錯誤");
        }
    }

    /// <summary>
    /// 分析缺失的索引建議
    /// </summary>
    private async Task AnalyzeMissingIndexesAsync()
    {
        const string sql = @"
            SELECT 
                query,
                calls,
                total_time,
                mean_time,
                rows
            FROM pg_stat_statements 
            WHERE query LIKE '%WHERE%' 
            OR query LIKE '%JOIN%'
            ORDER BY total_time DESC 
            LIMIT 10;
        ";

        try
        {
            // 注意：pg_stat_statements 需要額外安裝
            // 這裡提供基本的查詢分析
            _logger.LogInformation("檢查常見的查詢模式以建議索引...");
            
            // 分析 Tasks 表的查詢模式
            await AnalyzeTaskQueryPatternsAsync();
            
            // 分析 CaptureItems 表的查詢模式
            await AnalyzeCaptureItemQueryPatternsAsync();
            
            // 分析 TimerSessions 表的查詢模式
            await AnalyzeTimerSessionQueryPatternsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析缺失索引時發生錯誤");
        }
    }

    /// <summary>
    /// 分析索引碎片化
    /// </summary>
    private async Task AnalyzeIndexFragmentationAsync()
    {
        const string sql = @"
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
            ORDER BY pg_relation_size(indexrelid) DESC;
        ";

        try
        {
            var indexStats = await _context.Database.SqlQueryRaw<IndexOptimizationInfo>(sql).ToListAsync();
            
            foreach (var stat in indexStats.Take(10))
            {
                _logger.LogInformation("索引 {Schema}.{Table}.{Index}: 大小 {Size}", 
                    stat.SchemaName, stat.TableName, stat.IndexName, stat.IndexSize);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析索引碎片化時發生錯誤");
        }
    }

    /// <summary>
    /// 更新統計資訊
    /// </summary>
    private async Task UpdateStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("更新資料庫統計資訊...");
            await _context.Database.ExecuteSqlRawAsync("ANALYZE;");
            _logger.LogInformation("統計資訊更新完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新統計資訊時發生錯誤");
        }
    }

    /// <summary>
    /// 分析 Tasks 表的查詢模式
    /// </summary>
    private async Task AnalyzeTaskQueryPatternsAsync()
    {
        // 檢查是否需要複合索引
        var taskQueryPatterns = new[]
        {
            "檢查 UserId + Status 查詢頻率",
            "檢查 UserId + DueDate 查詢頻率", 
            "檢查 UserId + Priority 查詢頻率",
            "檢查 ParentTaskId 查詢頻率"
        };

        foreach (var pattern in taskQueryPatterns)
        {
            _logger.LogInformation("分析: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// 分析 CaptureItems 表的查詢模式
    /// </summary>
    private async Task AnalyzeCaptureItemQueryPatternsAsync()
    {
        var captureItemPatterns = new[]
        {
            "檢查 UserId + IsProcessed 查詢頻率",
            "檢查 UserId + Type 查詢頻率",
            "檢查 CreatedAt 範圍查詢頻率"
        };

        foreach (var pattern in captureItemPatterns)
        {
            _logger.LogInformation("分析: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// 分析 TimerSessions 表的查詢模式
    /// </summary>
    private async Task AnalyzeTimerSessionQueryPatternsAsync()
    {
        var timerSessionPatterns = new[]
        {
            "檢查 UserId + StartTime 查詢頻率",
            "檢查 UserId + Type 查詢頻率",
            "檢查 TaskId 查詢頻率"
        };

        foreach (var pattern in timerSessionPatterns)
        {
            _logger.LogInformation("分析: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// 創建建議的索引
    /// </summary>
    public async Task CreateRecommendedIndexesAsync()
    {
        try
        {
            _logger.LogInformation("創建建議的性能索引...");

            var indexCreationCommands = new[]
            {
                // 為頻繁查詢創建複合索引
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_Tasks_UserId_Status_Priority_DueDate"" 
                  ON ""Tasks"" (""UserId"", ""Status"", ""Priority"", ""DueDate"") 
                  WHERE ""Status"" != 3;", // 排除已完成的任務

                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_CaptureItems_UserId_IsProcessed_Type"" 
                  ON ""CaptureItems"" (""UserId"", ""IsProcessed"", ""Type"") 
                  WHERE ""IsProcessed"" = false;", // 只索引未處理的項目

                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_TimerSessions_UserId_StartTime_Type"" 
                  ON ""TimerSessions"" (""UserId"", ""StartTime"" DESC, ""Type"");",

                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_RefreshTokens_UserId_IsRevoked_ExpiresAt"" 
                  ON ""RefreshTokens"" (""UserId"", ""IsRevoked"", ""ExpiresAt"") 
                  WHERE ""IsRevoked"" = false;",

                // 為時間範圍查詢創建索引
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_UserProgress_UserId_Date_DESC"" 
                  ON ""UserProgress"" (""UserId"", ""Date"" DESC);",

                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_TimeBlocks_UserId_StartTime_EndTime"" 
                  ON ""TimeBlocks"" (""UserId"", ""StartTime"", ""EndTime"") 
                  WHERE ""StartTime"" >= CURRENT_DATE - INTERVAL '30 days';" // 只索引最近30天
            };

            foreach (var command in indexCreationCommands)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(command);
                    _logger.LogInformation("成功創建索引");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "創建索引時發生錯誤: {Command}", command);
                }
            }

            _logger.LogInformation("索引創建完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "創建建議索引時發生錯誤");
            throw;
        }
    }
}

/// <summary>
/// 未使用索引資訊
/// </summary>
public class UnusedIndexInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public long IdxTupRead { get; set; }
    public long IdxTupFetch { get; set; }
    public long IdxScan { get; set; }
}

/// <summary>
/// 索引統計資訊
/// </summary>
public class IndexOptimizationInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string IndexSize { get; set; } = string.Empty;
    public decimal BloatRatio { get; set; }
}