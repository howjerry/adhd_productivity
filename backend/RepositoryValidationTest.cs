using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AdhdProductivitySystem.Tests;

/// <summary>
/// 驗證重構後的 Repository Pattern 和 Unit of Work Pattern 功能的簡單測試
/// </summary>
public class RepositoryValidationTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("開始驗證後端核心功能...\n");

        await TestRepositoryPattern();
        await TestUnitOfWorkPattern();
        await TestGetTasksQueryHandlerOptimization();

        Console.WriteLine("\n✅ 所有測試完成！");
    }

    /// <summary>
    /// 測試 Repository Pattern 實作
    /// </summary>
    private static async Task TestRepositoryPattern()
    {
        Console.WriteLine("🔍 測試 Repository Pattern 實作...");

        // 設定 InMemory 資料庫
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new Repository<TaskItem>(context);

        // 測試基本 CRUD 操作
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "測試任務",
            Description = "Repository Pattern 測試",
            UserId = Guid.NewGuid(),
            Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Todo,
            Priority = AdhdProductivitySystem.Domain.Enums.Priority.Medium
        };

        // 測試新增
        repository.Add(task);
        await context.SaveChangesAsync();

        // 測試查詢
        var retrievedTask = await repository.GetByIdAsync(task.Id);
        if (retrievedTask?.Title != "測試任務")
        {
            throw new Exception("Repository GetByIdAsync 功能異常");
        }

        // 測試更新
        retrievedTask.Title = "更新後的任務";
        repository.Update(retrievedTask);
        await context.SaveChangesAsync();

        var updatedTask = await repository.GetByIdAsync(task.Id);
        if (updatedTask?.Title != "更新後的任務")
        {
            throw new Exception("Repository Update 功能異常");
        }

        // 測試查詢操作
        var tasks = await repository.GetListAsync(t => t.UserId == task.UserId);
        if (tasks.Count != 1)
        {
            throw new Exception("Repository GetListAsync 功能異常");
        }

        Console.WriteLine("✅ Repository Pattern 測試通過");
    }

    /// <summary>
    /// 測試 Unit of Work Pattern 實作
    /// </summary>
    private static async Task TestUnitOfWorkPattern()
    {
        Console.WriteLine("🔍 測試 Unit of Work Pattern 實作...");

        // 設定 InMemory 資料庫
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<UnitOfWork>>().Object;
        var unitOfWork = new UnitOfWork(context, logger);

        var userId = Guid.NewGuid();

        // 測試交易功能
        var isTransactionWorking = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 在交易中創建主任務
            var mainTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "主任務",
                UserId = userId,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Todo,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.High
            };

            unitOfWork.Tasks.Add(mainTask);

            // 創建子任務
            var subTask1 = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "子任務 1",
                UserId = userId,
                ParentTaskId = mainTask.Id,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Todo,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.Medium
            };

            var subTask2 = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "子任務 2",
                UserId = userId,
                ParentTaskId = mainTask.Id,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.InProgress,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.Low
            };

            unitOfWork.Tasks.Add(subTask1);
            unitOfWork.Tasks.Add(subTask2);

            return true;
        });

        if (!isTransactionWorking)
        {
            throw new Exception("UnitOfWork ExecuteInTransactionAsync 功能異常");
        }

        // 驗證交易完成後數據存在
        var tasks = await unitOfWork.Tasks.GetListAsync(t => t.UserId == userId);
        if (tasks.Count != 3)
        {
            throw new Exception($"UnitOfWork 交易功能異常，期望 3 個任務，實際 {tasks.Count} 個");
        }

        // 測試 Repository 重用
        var repo1 = unitOfWork.Repository<TaskItem>();
        var repo2 = unitOfWork.Repository<TaskItem>();
        if (!ReferenceEquals(repo1, repo2))
        {
            throw new Exception("UnitOfWork Repository 重用功能異常");
        }

        Console.WriteLine("✅ Unit of Work Pattern 測試通過");
    }

    /// <summary>
    /// 測試 GetTasksQueryHandler 的 N+1 查詢優化
    /// </summary>
    private static async Task TestGetTasksQueryHandlerOptimization()
    {
        Console.WriteLine("🔍 測試 GetTasksQueryHandler N+1 查詢優化...");

        // 設定 InMemory 資料庫
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);
        var taskRepository = new TaskRepository(context);

        var userId = Guid.NewGuid();

        // 創建測試資料：主任務和子任務
        var mainTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "主任務",
            UserId = userId,
            Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.InProgress,
            Priority = AdhdProductivitySystem.Domain.Enums.Priority.High
        };

        var subTask1 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "子任務 1",
            UserId = userId,
            ParentTaskId = mainTask.Id,
            Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Completed,
            Priority = AdhdProductivitySystem.Domain.Enums.Priority.Medium
        };

        var subTask2 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "子任務 2",
            UserId = userId,
            ParentTaskId = mainTask.Id,
            Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.InProgress,
            Priority = AdhdProductivitySystem.Domain.Enums.Priority.Low
        };

        context.Tasks.AddRange(mainTask, subTask1, subTask2);
        await context.SaveChangesAsync();

        // 測試優化後的查詢方法
        var taskDtos = await taskRepository.GetTasksWithStatisticsAsync(
            userId: userId,
            page: 1,
            pageSize: 10
        );

        if (taskDtos.Count == 0)
        {
            throw new Exception("GetTasksWithStatisticsAsync 未返回任務");
        }

        // 驗證子任務統計正確計算
        var mainTaskDto = taskDtos.FirstOrDefault(t => t.Title == "主任務");
        if (mainTaskDto == null)
        {
            throw new Exception("未找到主任務");
        }

        if (mainTaskDto.SubTaskCount != 2)
        {
            throw new Exception($"子任務數量錯誤，期望 2，實際 {mainTaskDto.SubTaskCount}");
        }

        if (mainTaskDto.CompletedSubTaskCount != 1)
        {
            throw new Exception($"已完成子任務數量錯誤，期望 1，實際 {mainTaskDto.CompletedSubTaskCount}");
        }

        // 測試篩選功能
        var inProgressTasks = await taskRepository.GetTasksWithStatisticsAsync(
            userId: userId,
            status: AdhdProductivitySystem.Domain.Enums.TaskStatus.InProgress,
            page: 1,
            pageSize: 10
        );

        if (inProgressTasks.Count != 2) // 主任務 + 子任務2
        {
            throw new Exception($"狀態篩選功能異常，期望 2 個 InProgress 任務，實際 {inProgressTasks.Count} 個");
        }

        Console.WriteLine("✅ GetTasksQueryHandler N+1 查詢優化測試通過");
    }
}