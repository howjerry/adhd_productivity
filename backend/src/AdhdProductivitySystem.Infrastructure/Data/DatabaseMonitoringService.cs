using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// 資料庫監控和效能追蹤服務
/// </summary>
public class DatabaseMonitoringService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseMonitoringService> _logger;

    public DatabaseMonitoringService(ApplicationDbContext context, ILogger<DatabaseMonitoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 獲取資料庫效能指標
    /// </summary>
    public async Task<DatabasePerformanceMetrics> GetPerformanceMetricsAsync()
    {
        var metrics = new DatabasePerformanceMetrics();

        try
        {
            _logger.LogInformation("收集資料庫效能指標...");

            // 收集連線統計
            await CollectConnectionStatsAsync(metrics);

            // 收集查詢統計
            await CollectQueryStatsAsync(metrics);

            // 收集資料庫大小統計
            await CollectDatabaseSizeStatsAsync(metrics);

            // 收集表統計
            await CollectTableStatsAsync(metrics);

            // 收集索引統計
            await CollectIndexStatsAsync(metrics);

            // 收集鎖定統計
            await CollectLockStatsAsync(metrics);

            // 收集 I/O 統計
            await CollectIOStatsAsync(metrics);

            metrics.CollectedAt = DateTime.UtcNow;
            _logger.LogInformation("資料庫效能指標收集完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收集資料庫效能指標時發生錯誤");
            throw;
        }

        return metrics;
    }

    /// <summary>
    /// 收集連線統計
    /// </summary>
    private async Task CollectConnectionStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            // 活躍連線數
            var activeConnections = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM pg_stat_activity
                WHERE state = 'active'
                AND datname = current_database()
            ").FirstOrDefaultAsync();

            // 總連線數
            var totalConnections = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM pg_stat_activity
                WHERE datname = current_database()
            ").FirstOrDefaultAsync();

            // 最大連線數
            var maxConnections = await _context.Database.SqlQueryRaw<int>(@"
                SELECT setting::int as Value
                FROM pg_settings
                WHERE name = 'max_connections'
            ").FirstOrDefaultAsync();

            metrics.ActiveConnections = activeConnections;
            metrics.TotalConnections = totalConnections;
            metrics.MaxConnections = maxConnections;
            metrics.ConnectionUtilizationPercent = totalConnections * 100.0 / maxConnections;

            _logger.LogInformation("連線統計 - 活躍: {Active}, 總數: {Total}, 最大: {Max}, 使用率: {Utilization:F1}%",
                activeConnections, totalConnections, maxConnections, metrics.ConnectionUtilizationPercent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集連線統計時發生錯誤");
        }
    }

    /// <summary>
    /// 收集查詢統計
    /// </summary>
    private async Task CollectQueryStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            // 慢查詢統計
            var slowQueries = await _context.Database.SqlQueryRaw<SlowQueryInfo>(@"
                SELECT 
                    query,
                    calls,
                    total_time,
                    mean_time,
                    max_time,
                    stddev_time,
                    rows
                FROM pg_stat_statements 
                WHERE mean_time > 100  -- 大於100ms的查詢
                ORDER BY mean_time DESC 
                LIMIT 10
            ").ToListAsync();

            metrics.SlowQueries = slowQueries;

            // 資料庫統計
            var dbStats = await _context.Database.SqlQueryRaw<DatabaseStats>(@"
                SELECT 
                    numbackends as ActiveBackends,
                    xact_commit as CommittedTransactions,
                    xact_rollback as RolledBackTransactions,
                    blks_read as BlocksRead,
                    blks_hit as BlocksHit,
                    tup_returned as TuplesReturned,
                    tup_fetched as TuplesFetched,
                    tup_inserted as TuplesInserted,
                    tup_updated as TuplesUpdated,
                    tup_deleted as TuplesDeleted
                FROM pg_stat_database 
                WHERE datname = current_database()
            ").FirstOrDefaultAsync();

            if (dbStats != null)
            {
                metrics.DatabaseStats = dbStats;
                
                // 計算緩存命中率
                var totalReads = dbStats.BlocksRead + dbStats.BlocksHit;
                metrics.CacheHitRatio = totalReads > 0 ? (double)dbStats.BlocksHit / totalReads * 100 : 0;
            }

            _logger.LogInformation("查詢統計收集完成 - 慢查詢數: {SlowQueryCount}, 緩存命中率: {CacheHitRatio:F2}%",
                slowQueries.Count, metrics.CacheHitRatio);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集查詢統計時發生錯誤");
        }
    }

    /// <summary>
    /// 收集資料庫大小統計
    /// </summary>
    private async Task CollectDatabaseSizeStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            // 資料庫總大小
            var databaseSize = await _context.Database.SqlQueryRaw<long>(@"
                SELECT pg_database_size(current_database()) as Size
            ").FirstOrDefaultAsync();

            metrics.DatabaseSizeBytes = databaseSize;
            metrics.DatabaseSizeMB = databaseSize / (1024.0 * 1024.0);

            _logger.LogInformation("資料庫大小: {Size:F2} MB", metrics.DatabaseSizeMB);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集資料庫大小統計時發生錯誤");
        }
    }

    /// <summary>
    /// 收集表統計
    /// </summary>
    private async Task CollectTableStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            var tableStats = await _context.Database.SqlQueryRaw<TableStatsInfo>(@"
                SELECT 
                    schemaname as SchemaName,
                    relname as TableName,
                    n_tup_ins as TuplesInserted,
                    n_tup_upd as TuplesUpdated,
                    n_tup_del as TuplesDeleted,
                    n_live_tup as LiveTuples,
                    n_dead_tup as DeadTuples,
                    last_vacuum,
                    last_autovacuum,
                    last_analyze,
                    last_autoanalyze,
                    pg_size_pretty(pg_relation_size(relid)) as TableSize
                FROM pg_stat_user_tables
                ORDER BY n_live_tup DESC
            ").ToListAsync();

            metrics.TableStats = tableStats;

            // 檢查需要 VACUUM 的表
            var tablesNeedingVacuum = tableStats.Where(t => 
                t.DeadTuples > 0 && 
                (t.DeadTuples * 100.0 / (t.LiveTuples + t.DeadTuples)) > 10 // 超過10%死元組
            ).ToList();

            if (tablesNeedingVacuum.Any())
            {
                _logger.LogWarning("發現 {Count} 個表需要 VACUUM: {Tables}",
                    tablesNeedingVacuum.Count,
                    string.Join(", ", tablesNeedingVacuum.Select(t => t.TableName)));
            }

            _logger.LogInformation("表統計收集完成 - 總表數: {TableCount}", tableStats.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集表統計時發生錯誤");
        }
    }

    /// <summary>
    /// 收集索引統計
    /// </summary>
    private async Task CollectIndexStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            var indexStats = await _context.Database.SqlQueryRaw<IndexStatsInfo>(@"
                SELECT 
                    schemaname as SchemaName,
                    relname as TableName,
                    indexrelname as IndexName,
                    idx_scan as IndexScans,
                    idx_tup_read as TuplesRead,
                    idx_tup_fetch as TuplesFetched,
                    pg_size_pretty(pg_relation_size(indexrelid)) as IndexSize
                FROM pg_stat_user_indexes
                ORDER BY idx_scan DESC
            ").ToListAsync();

            metrics.IndexStats = indexStats;

            // 檢查未使用的索引
            var unusedIndexes = indexStats.Where(i => i.IndexScans == 0).ToList();
            if (unusedIndexes.Any())
            {
                _logger.LogWarning("發現 {Count} 個未使用的索引: {Indexes}",
                    unusedIndexes.Count,
                    string.Join(", ", unusedIndexes.Select(i => $"{i.TableName}.{i.IndexName}")));
            }

            _logger.LogInformation("索引統計收集完成 - 總索引數: {IndexCount}, 未使用索引: {UnusedCount}",
                indexStats.Count, unusedIndexes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集索引統計時發生錯誤");
        }
    }

    /// <summary>
    /// 收集鎖定統計
    /// </summary>
    private async Task CollectLockStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            var lockStats = await _context.Database.SqlQueryRaw<LockStatsInfo>(@"
                SELECT 
                    mode as LockMode,
                    COUNT(*) as LockCount
                FROM pg_locks
                WHERE database = (SELECT oid FROM pg_database WHERE datname = current_database())
                GROUP BY mode
                ORDER BY COUNT(*) DESC
            ").ToListAsync();

            metrics.LockStats = lockStats;

            // 檢查是否有阻塞的查詢
            var blockedQueries = await _context.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) as Count
                FROM pg_stat_activity
                WHERE wait_event_type = 'Lock'
                AND datname = current_database()
            ").FirstOrDefaultAsync();

            metrics.BlockedQueries = blockedQueries;

            if (blockedQueries > 0)
            {
                _logger.LogWarning("發現 {Count} 個被阻塞的查詢", blockedQueries);
            }

            _logger.LogInformation("鎖定統計收集完成 - 總鎖數: {LockCount}, 阻塞查詢: {BlockedCount}",
                lockStats.Sum(l => l.LockCount), blockedQueries);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集鎖定統計時發生錯誤");
        }
    }

    /// <summary>
    /// 收集 I/O 統計
    /// </summary>
    private async Task CollectIOStatsAsync(DatabasePerformanceMetrics metrics)
    {
        try
        {
            var ioStats = await _context.Database.SqlQueryRaw<IOStatsInfo>(@"
                SELECT 
                    heap_blks_read as HeapBlocksRead,
                    heap_blks_hit as HeapBlocksHit,
                    idx_blks_read as IndexBlocksRead,
                    idx_blks_hit as IndexBlocksHit,
                    toast_blks_read as ToastBlocksRead,
                    toast_blks_hit as ToastBlocksHit
                FROM pg_statio_user_tables
                WHERE schemaname = 'public'
            ").ToListAsync();

            if (ioStats.Any())
            {
                var totalHeapReads = ioStats.Sum(s => s.HeapBlocksRead + s.HeapBlocksHit);
                var totalHeapHits = ioStats.Sum(s => s.HeapBlocksHit);
                var totalIndexReads = ioStats.Sum(s => s.IndexBlocksRead + s.IndexBlocksHit);
                var totalIndexHits = ioStats.Sum(s => s.IndexBlocksHit);

                metrics.HeapCacheHitRatio = totalHeapReads > 0 ? totalHeapHits * 100.0 / totalHeapReads : 0;
                metrics.IndexCacheHitRatio = totalIndexReads > 0 ? totalIndexHits * 100.0 / totalIndexReads : 0;
            }

            _logger.LogInformation("I/O 統計收集完成 - 堆緩存命中率: {HeapHitRatio:F2}%, 索引緩存命中率: {IndexHitRatio:F2}%",
                metrics.HeapCacheHitRatio, metrics.IndexCacheHitRatio);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "收集 I/O 統計時發生錯誤");
        }
    }

    /// <summary>
    /// 檢查資料庫健康狀況
    /// </summary>
    public async Task<DatabaseHealthStatus> CheckHealthAsync()
    {
        var health = new DatabaseHealthStatus();

        try
        {
            var metrics = await GetPerformanceMetricsAsync();

            // 評估健康狀況
            health.IsHealthy = true;
            health.Issues = new List<string>();

            // 檢查連線使用率
            if (metrics.ConnectionUtilizationPercent > 80)
            {
                health.IsHealthy = false;
                health.Issues.Add($"連線使用率過高: {metrics.ConnectionUtilizationPercent:F1}%");
            }

            // 檢查緩存命中率
            if (metrics.CacheHitRatio < 95)
            {
                health.IsHealthy = false;
                health.Issues.Add($"緩存命中率過低: {metrics.CacheHitRatio:F2}%");
            }

            // 檢查阻塞查詢
            if (metrics.BlockedQueries > 0)
            {
                health.IsHealthy = false;
                health.Issues.Add($"存在 {metrics.BlockedQueries} 個阻塞查詢");
            }

            // 檢查慢查詢
            if (metrics.SlowQueries.Count > 5)
            {
                health.Issues.Add($"發現 {metrics.SlowQueries.Count} 個慢查詢");
            }

            health.CheckedAt = DateTime.UtcNow;
            health.Metrics = metrics;

            _logger.LogInformation("資料庫健康檢查完成 - 狀態: {Status}", 
                health.IsHealthy ? "健康" : "有問題");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料庫健康檢查時發生錯誤");
            health.IsHealthy = false;
            health.Issues.Add($"健康檢查失敗: {ex.Message}");
        }

        return health;
    }
}

// 資料模型類別
public class DatabasePerformanceMetrics
{
    public DateTime CollectedAt { get; set; }
    
    // 連線統計
    public int ActiveConnections { get; set; }
    public int TotalConnections { get; set; }
    public int MaxConnections { get; set; }
    public double ConnectionUtilizationPercent { get; set; }
    
    // 查詢統計
    public double CacheHitRatio { get; set; }
    public int BlockedQueries { get; set; }
    public List<SlowQueryInfo> SlowQueries { get; set; } = new();
    public DatabaseStats? DatabaseStats { get; set; }
    
    // 大小統計
    public long DatabaseSizeBytes { get; set; }
    public double DatabaseSizeMB { get; set; }
    
    // 表和索引統計
    public List<TableStatsInfo> TableStats { get; set; } = new();
    public List<IndexStatsInfo> IndexStats { get; set; } = new();
    public List<LockStatsInfo> LockStats { get; set; } = new();
    
    // I/O 統計
    public double HeapCacheHitRatio { get; set; }
    public double IndexCacheHitRatio { get; set; }
}

public class SlowQueryInfo
{
    public string Query { get; set; } = string.Empty;
    public long Calls { get; set; }
    public double TotalTime { get; set; }
    public double MeanTime { get; set; }
    public double MaxTime { get; set; }
    public double StddevTime { get; set; }
    public long Rows { get; set; }
}

public class DatabaseStats
{
    public int ActiveBackends { get; set; }
    public long CommittedTransactions { get; set; }
    public long RolledBackTransactions { get; set; }
    public long BlocksRead { get; set; }
    public long BlocksHit { get; set; }
    public long TuplesReturned { get; set; }
    public long TuplesFetched { get; set; }
    public long TuplesInserted { get; set; }
    public long TuplesUpdated { get; set; }
    public long TuplesDeleted { get; set; }
}

public class TableStatsInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long TuplesInserted { get; set; }
    public long TuplesUpdated { get; set; }
    public long TuplesDeleted { get; set; }
    public long LiveTuples { get; set; }
    public long DeadTuples { get; set; }
    public DateTime? LastVacuum { get; set; }
    public DateTime? LastAutovacuum { get; set; }
    public DateTime? LastAnalyze { get; set; }
    public DateTime? LastAutoanalyze { get; set; }
    public string TableSize { get; set; } = string.Empty;
}

public class IndexStatsInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public long IndexScans { get; set; }
    public long TuplesRead { get; set; }
    public long TuplesFetched { get; set; }
    public string IndexSize { get; set; } = string.Empty;
}

public class LockStatsInfo
{
    public string LockMode { get; set; } = string.Empty;
    public int LockCount { get; set; }
}

public class IOStatsInfo
{
    public long HeapBlocksRead { get; set; }
    public long HeapBlocksHit { get; set; }
    public long IndexBlocksRead { get; set; }
    public long IndexBlocksHit { get; set; }
    public long ToastBlocksRead { get; set; }
    public long ToastBlocksHit { get; set; }
}

public class DatabaseHealthStatus
{
    public bool IsHealthy { get; set; } = true;
    public DateTime CheckedAt { get; set; }
    public List<string> Issues { get; set; } = new();
    public DatabasePerformanceMetrics? Metrics { get; set; }
}