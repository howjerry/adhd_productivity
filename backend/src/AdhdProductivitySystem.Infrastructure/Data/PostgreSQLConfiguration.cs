using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// PostgreSQL 特定配置
/// </summary>
public static class PostgreSQLConfiguration
{
    /// <summary>
    /// 配置 PostgreSQL 特定設定
    /// </summary>
    public static void ConfigurePostgreSQL(this ModelBuilder modelBuilder)
    {
        // 設定 PostgreSQL 特定的命名慣例
        // 使用 snake_case 命名（可選）
        // modelBuilder.UseSnakeCaseNamingConvention();
        
        // 配置 PostgreSQL 擴展
        modelBuilder.HasPostgresExtension("uuid-ossp");
        
        // 配置序列（如果需要）
        // modelBuilder.HasSequence<int>("task_sequence");
        
        // 配置資料庫特定的索引和約束
        ConfigureIndexes(modelBuilder);
        ConfigureConstraints(modelBuilder);
    }
    
    /// <summary>
    /// 配置 PostgreSQL 特定的索引
    /// </summary>
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // 配置部分索引（PostgreSQL 特有）
        // 例如：只為未刪除的記錄建立索引
        
        // 為 RefreshToken 配置部分索引（只為未撤銷的 token 建立索引）
        modelBuilder.Entity<Domain.Entities.RefreshToken>()
            .HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt_NotRevoked")
            .HasFilter("\"IsRevoked\" = false");
            
        // 為 Tasks 配置複合索引
        modelBuilder.Entity<Domain.Entities.TaskItem>()
            .HasIndex(t => new { t.UserId, t.Status, t.DueDate })
            .HasDatabaseName("IX_Tasks_UserId_Status_DueDate_Optimized");
            
        // 為 CaptureItems 配置索引
        modelBuilder.Entity<Domain.Entities.CaptureItem>()
            .HasIndex(ci => new { ci.UserId, ci.IsProcessed, ci.CreatedAt })
            .HasDatabaseName("IX_CaptureItems_UserId_IsProcessed_CreatedAt");
    }
    
    /// <summary>
    /// 配置 PostgreSQL 特定的約束
    /// </summary>
    private static void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        // 配置檢查約束
        
        // RefreshToken 的約束
        modelBuilder.Entity<Domain.Entities.RefreshToken>()
            .HasCheckConstraint("CK_RefreshToken_ExpiresAt_Future", 
                "\"ExpiresAt\" > \"CreatedAt\"");
                
        // Task 的約束
        modelBuilder.Entity<Domain.Entities.TaskItem>()
            .HasCheckConstraint("CK_Task_EstimatedMinutes_Positive", 
                "\"EstimatedMinutes\" IS NULL OR \"EstimatedMinutes\" > 0");
                
        modelBuilder.Entity<Domain.Entities.TaskItem>()
            .HasCheckConstraint("CK_Task_ActualMinutes_NonNegative", 
                "\"ActualMinutes\" >= 0");
                
        // TimerSession 的約束
        modelBuilder.Entity<Domain.Entities.TimerSession>()
            .HasCheckConstraint("CK_TimerSession_PlannedMinutes_Positive", 
                "\"PlannedMinutes\" > 0");
                
        modelBuilder.Entity<Domain.Entities.TimerSession>()
            .HasCheckConstraint("CK_TimerSession_EndTime_After_StartTime", 
                "\"EndTime\" IS NULL OR \"EndTime\" > \"StartTime\"");
                
        // UserProgress 的約束
        modelBuilder.Entity<Domain.Entities.UserProgress>()
            .HasCheckConstraint("CK_UserProgress_HoursSlept_Valid", 
                "\"HoursSlept\" >= 0 AND \"HoursSlept\" <= 24");
                
        modelBuilder.Entity<Domain.Entities.UserProgress>()
            .HasCheckConstraint("CK_UserProgress_Metrics_Valid", 
                "\"Mood\" BETWEEN 1 AND 10 AND \"EnergyLevel\" BETWEEN 1 AND 10 AND \"FocusLevel\" BETWEEN 1 AND 10 AND \"StressLevel\" BETWEEN 1 AND 10 AND \"SleepQuality\" BETWEEN 1 AND 10");
    }
    
    /// <summary>
    /// 配置連線字串的 PostgreSQL 特定參數
    /// </summary>
    public static string ConfigureConnectionString(string baseConnectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        
        // 設定連線池參數
        builder.MaxPoolSize = 100;
        builder.MinPoolSize = 5;
        builder.ConnectionIdleLifetime = 300; // 5 分鐘
        builder.ConnectionPruningInterval = 10; // 10 秒
        
        // 設定超時參數
        builder.Timeout = 30;
        builder.CommandTimeout = 60;
        
        // 設定 SSL 參數（生產環境建議開啟）
        // builder.SslMode = SslMode.Require;
        
        // 設定時區處理
        builder.Timezone = "UTC";
        
        // 啟用性能相關功能
        builder.IncludeErrorDetail = true;
        builder.LogParameters = false; // 生產環境應設為 false
        
        return builder.ToString();
    }
}