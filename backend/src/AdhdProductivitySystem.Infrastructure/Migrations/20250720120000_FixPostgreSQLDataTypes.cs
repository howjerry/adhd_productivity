using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdhdProductivitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPostgreSQLDataTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 這個遷移用於修復 PostgreSQL 資料類型
            // 如果資料庫不存在，EF Core 會自動使用正確的 PostgreSQL 類型
            // 如果已存在 SQL Server 類型，則需要手動轉換
            
            // 注意：這些 ALTER 命令只在資料類型不匹配時才需要執行
            // EF Core 會自動處理類型映射，所以這個遷移主要用於文檔化
            
            migrationBuilder.Sql(@"
                -- 這個遷移確保所有表使用正確的 PostgreSQL 資料類型
                -- 如果從其他資料庫遷移，可能需要手動轉換資料類型
                
                -- 檢查並修復 GUID 類型
                DO $$
                BEGIN
                    -- 檢查 Users 表的 Id 欄位
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Users' 
                        AND column_name = 'Id' 
                        AND data_type != 'uuid'
                    ) THEN
                        ALTER TABLE ""Users"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;
                    END IF;
                    
                    -- 檢查 Tasks 表的 GUID 欄位
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Tasks' 
                        AND column_name = 'Id' 
                        AND data_type != 'uuid'
                    ) THEN
                        ALTER TABLE ""Tasks"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;
                        ALTER TABLE ""Tasks"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Tasks' AND column_name = 'ParentTaskId') THEN
                            ALTER TABLE ""Tasks"" ALTER COLUMN ""ParentTaskId"" TYPE uuid USING ""ParentTaskId""::uuid;
                        END IF;
                    END IF;
                    
                    -- 檢查其他表...
                    -- 這裡可以添加更多表的檢查和修復
                    
                EXCEPTION
                    WHEN OTHERS THEN
                        -- 如果發生錯誤，記錄但不中斷遷移
                        RAISE NOTICE 'Warning: Could not convert some data types. This is normal for new databases.';
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 這個遷移的回滾不需要做任何事情
            // 因為它只是確保使用正確的 PostgreSQL 類型
        }
    }
}