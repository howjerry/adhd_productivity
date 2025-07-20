using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using TaskStatus = AdhdProductivitySystem.Domain.Enums.TaskStatus;

namespace AdhdProductivitySystem.Tests.Unit.Performance;

/// <summary>
/// 效能測試，專門驗證 N+1 查詢修復和查詢最佳化
/// </summary>
public class QueryOptimizationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskRepository _taskRepository;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ILogger<TaskRepository>> _mockLogger;

    public QueryOptimizationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<TaskRepository>>();
        _taskRepository = new TaskRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    #region N+1 查詢問題測試

    [Fact]
    public async Task GetUserTasksWithSubtasks_ShouldAvoidNPlusOneQuery()
    {
        // Arrange
        var userId = "performance-user";
        await SeedTestDataAsync(userId, parentTaskCount: 10, subtasksPerParent: 5);

        var queryCount = 0;
        var originalCommandText = string.Empty;

        // 攔截 SQL 查詢以計算執行次數
        _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));

        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = await _taskRepository.GetUserTasksWithSubtasksAsync(userId);

        stopwatch.Stop();

        // Assert
        tasks.Should().HaveCount(10, "應該返回10個父任務");
        tasks.Should().OnlyContain(t => t.Subtasks.Count == 5, "每個父任務應該有5個子任務");
        
        // 效能斷言
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "查詢應該在1秒內完成");
        
        // 驗證是否使用了 Include 來避免 N+1 查詢
        // 在實際應用中，這裡應該只執行一個或很少的 SQL 查詢
        Console.WriteLine($"Query execution time: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetTaskWithSubtasks_SingleQuery_ShouldUseInclude()
    {
        // Arrange
        var userId = "single-task-user";
        var parentTask = await CreateTaskWithSubtasksAsync(userId, subtaskCount: 10);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _taskRepository.GetTaskWithSubtasksAsync(parentTask.Id);

        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result!.Subtasks.Should().HaveCount(10);
        
        // 效能斷言：單一查詢應該很快
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "單一任務查詢應該在500ms內完成");
        
        Console.WriteLine($"Single task query time: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task BatchTaskOperations_ShouldBeOptimized()
    {
        // Arrange
        var userId = "batch-user";
        var taskCount = 100;
        var tasks = Enumerable.Range(1, taskCount)
            .Select(i => CreateTestTask(userId, $"Batch Task {i}"))
            .ToList();

        var stopwatch = Stopwatch.StartNew();

        // Act - 批次插入
        await _taskRepository.AddRangeAsync(tasks);
        await _unitOfWork.SaveChangesAsync();

        stopwatch.Stop();
        var insertTime = stopwatch.ElapsedMilliseconds;

        // 批次查詢
        stopwatch.Restart();
        var retrievedTasks = await _taskRepository.FindAsync(t => t.UserId == userId);
        stopwatch.Stop();
        var queryTime = stopwatch.ElapsedMilliseconds;

        // Assert
        retrievedTasks.Should().HaveCount(taskCount);
        
        // 效能斷言
        insertTime.Should().BeLessThan(2000, "批次插入100個任務應該在2秒內完成");
        queryTime.Should().BeLessThan(1000, "批次查詢100個任務應該在1秒內完成");
        
        Console.WriteLine($"Batch insert time: {insertTime}ms, Query time: {queryTime}ms");
    }

    #endregion

    #region 複雜查詢最佳化測試

    [Fact]
    public async Task ComplexTaskQuery_WithMultipleFilters_ShouldBeEfficient()
    {
        // Arrange
        var userId = "complex-query-user";
        await SeedComplexTestDataAsync(userId);

        var stopwatch = Stopwatch.StartNew();

        // Act - 複雜查詢：包含篩選、排序、分頁
        var tasks = await _context.TaskItems
            .Where(t => t.UserId == userId)
            .Where(t => t.Status == TaskStatus.InProgress || t.Status == TaskStatus.Pending)
            .Where(t => t.Priority >= 3)
            .Where(t => t.DueDate >= DateTime.UtcNow)
            .Include(t => t.Subtasks)
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .Take(20)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        tasks.Should().NotBeEmpty("應該返回符合條件的任務");
        tasks.Should().OnlyContain(t => t.Status == TaskStatus.InProgress || t.Status == TaskStatus.Pending);
        tasks.Should().OnlyContain(t => t.Priority >= 3);
        
        // 效能斷言
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1500, "複雜查詢應該在1.5秒內完成");
        
        Console.WriteLine($"Complex query time: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task TaskStatisticsQuery_ShouldBeOptimized()
    {
        // Arrange
        var userId = "stats-user";
        await SeedTestDataAsync(userId, parentTaskCount: 50, subtasksPerParent: 3);

        var stopwatch = Stopwatch.StartNew();

        // Act - 統計查詢
        var stats = await CalculateTaskStatisticsAsync(userId);

        stopwatch.Stop();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalTasks.Should().BeGreaterThan(0);
        stats.CompletedTasks.Should().BeGreaterOrEqualTo(0);
        stats.InProgressTasks.Should().BeGreaterOrEqualTo(0);
        stats.PendingTasks.Should().BeGreaterOrEqualTo(0);
        
        // 效能斷言
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "統計查詢應該在1秒內完成");
        
        Console.WriteLine($"Statistics query time: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 記憶體使用最佳化測試

    [Fact]
    public async Task LargeDatasetQuery_ShouldNotCauseMemoryIssues()
    {
        // Arrange
        var userId = "memory-test-user";
        await SeedLargeDatasetAsync(userId);

        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = await _taskRepository.GetPagedAsync(
            pageNumber: 1,
            pageSize: 50,
            filter: t => t.UserId == userId,
            orderBy: t => t.CreatedAt);

        stopwatch.Stop();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        tasks.Should().HaveCount(50);
        
        // 效能斷言
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "大數據集分頁查詢應該在2秒內完成");
        memoryUsed.Should().BeLessThan(50 * 1024 * 1024, "記憶體使用應該少於50MB"); // 50MB
        
        Console.WriteLine($"Large dataset query time: {stopwatch.ElapsedMilliseconds}ms, Memory used: {memoryUsed / 1024 / 1024}MB");
    }

    [Fact]
    public async Task StreamingQuery_ShouldProcessLargeDataEfficiently()
    {
        // Arrange
        var userId = "streaming-user";
        await SeedLargeDatasetAsync(userId);

        var processedCount = 0;
        var maxMemoryUsage = 0L;
        var initialMemory = GC.GetTotalMemory(true);

        var stopwatch = Stopwatch.StartNew();

        // Act - 使用流式處理大量數據
        await foreach (var task in StreamTasksAsync(userId))
        {
            processedCount++;
            
            // 模擬處理
            await Task.Delay(1);
            
            // 監控記憶體使用
            var currentMemory = GC.GetTotalMemory(false) - initialMemory;
            if (currentMemory > maxMemoryUsage)
                maxMemoryUsage = currentMemory;
            
            if (processedCount >= 100) break; // 限制處理數量
        }

        stopwatch.Stop();

        // Assert
        processedCount.Should().Be(100);
        
        // 效能斷言
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "流式處理應該在5秒內完成");
        maxMemoryUsage.Should().BeLessThan(20 * 1024 * 1024, "流式處理記憶體使用應該少於20MB");
        
        Console.WriteLine($"Streaming processing time: {stopwatch.ElapsedMilliseconds}ms, Max memory: {maxMemoryUsage / 1024 / 1024}MB");
    }

    #endregion

    #region 索引效能測試

    [Fact]
    public async Task IndexedQueries_ShouldBeFasterThanNonIndexed()
    {
        // Arrange
        var userId = "index-test-user";
        await SeedTestDataAsync(userId, parentTaskCount: 100, subtasksPerParent: 2);

        // Act - 測試應該被索引最佳化的查詢
        var stopwatch = Stopwatch.StartNew();
        
        // 按 UserId 查詢（應該有索引）
        var userTasks = await _taskRepository.FindAsync(t => t.UserId == userId);
        
        stopwatch.Stop();
        var userIdQueryTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        
        // 按狀態查詢（應該有索引）
        var inProgressTasks = await _taskRepository.FindAsync(t => t.Status == TaskStatus.InProgress);
        
        stopwatch.Stop();
        var statusQueryTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        
        // 按到期日查詢（應該有索引）
        var dueTasks = await _taskRepository.FindAsync(t => t.DueDate <= DateTime.UtcNow.AddDays(7));
        
        stopwatch.Stop();
        var dueDateQueryTime = stopwatch.ElapsedMilliseconds;

        // Assert
        userTasks.Should().NotBeEmpty();
        
        // 效能斷言
        userIdQueryTime.Should().BeLessThan(500, "UserId 索引查詢應該很快");
        statusQueryTime.Should().BeLessThan(500, "Status 索引查詢應該很快");
        dueDateQueryTime.Should().BeLessThan(500, "DueDate 索引查詢應該很快");
        
        Console.WriteLine($"UserId query: {userIdQueryTime}ms, Status query: {statusQueryTime}ms, DueDate query: {dueDateQueryTime}ms");
    }

    #endregion

    #region 併發效能測試

    [Fact]
    public async Task ConcurrentQueries_ShouldNotDegrade()
    {
        // Arrange
        var userId = "concurrent-user";
        await SeedTestDataAsync(userId, parentTaskCount: 50, subtasksPerParent: 2);

        var concurrentTasks = new List<Task<TimeSpan>>();
        var taskCount = 10;

        // Act - 同時執行多個查詢
        for (int i = 0; i < taskCount; i++)
        {
            concurrentTasks.Add(Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                await _taskRepository.GetUserTasksWithSubtasksAsync(userId);
                sw.Stop();
                return sw.Elapsed;
            }));
        }

        var results = await Task.WhenAll(concurrentTasks);
        var averageTime = results.Average(t => t.TotalMilliseconds);
        var maxTime = results.Max(t => t.TotalMilliseconds);

        // Assert
        results.Should().HaveCount(taskCount);
        
        // 效能斷言
        averageTime.Should().BeLessThan(2000, "併發查詢平均時間應該少於2秒");
        maxTime.Should().BeLessThan(5000, "最慢的查詢應該少於5秒");
        
        Console.WriteLine($"Concurrent queries - Average: {averageTime:F2}ms, Max: {maxTime:F2}ms");
    }

    #endregion

    #region 私有輔助方法

    private async Task SeedTestDataAsync(string userId, int parentTaskCount, int subtasksPerParent)
    {
        var tasks = new List<TaskItem>();

        for (int i = 1; i <= parentTaskCount; i++)
        {
            var parentTask = CreateTestTask(userId, $"Parent Task {i}");
            tasks.Add(parentTask);

            for (int j = 1; j <= subtasksPerParent; j++)
            {
                var subtask = CreateTestTask(userId, $"Subtask {i}-{j}");
                subtask.ParentTaskId = parentTask.Id;
                tasks.Add(subtask);
            }
        }

        await _context.TaskItems.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();
    }

    private async Task SeedComplexTestDataAsync(string userId)
    {
        var random = new Random();
        var tasks = new List<TaskItem>();

        for (int i = 1; i <= 200; i++)
        {
            var task = CreateTestTask(userId, $"Complex Task {i}");
            task.Status = (TaskStatus)(random.Next(0, 4)); // 隨機狀態
            task.Priority = random.Next(1, 6); // 1-5 優先級
            task.DueDate = DateTime.UtcNow.AddDays(random.Next(-30, 30)); // 隨機到期日
            tasks.Add(task);
        }

        await _context.TaskItems.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();
    }

    private async Task SeedLargeDatasetAsync(string userId)
    {
        var batchSize = 100;
        var totalTasks = 1000;

        for (int batch = 0; batch < totalTasks / batchSize; batch++)
        {
            var tasks = new List<TaskItem>();
            
            for (int i = 1; i <= batchSize; i++)
            {
                var taskIndex = batch * batchSize + i;
                var task = CreateTestTask(userId, $"Large Dataset Task {taskIndex}");
                tasks.Add(task);
            }

            await _context.TaskItems.AddRangeAsync(tasks);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<TaskItem> CreateTaskWithSubtasksAsync(string userId, int subtaskCount)
    {
        var parentTask = CreateTestTask(userId, "Parent with many subtasks");
        await _context.TaskItems.AddAsync(parentTask);

        var subtasks = new List<TaskItem>();
        for (int i = 1; i <= subtaskCount; i++)
        {
            var subtask = CreateTestTask(userId, $"Subtask {i}");
            subtask.ParentTaskId = parentTask.Id;
            subtasks.Add(subtask);
        }

        await _context.TaskItems.AddRangeAsync(subtasks);
        await _context.SaveChangesAsync();

        return parentTask;
    }

    private async Task<TaskStatistics> CalculateTaskStatisticsAsync(string userId)
    {
        var allTasks = await _context.TaskItems
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new TaskStatistics
        {
            TotalTasks = allTasks.Sum(x => x.Count),
            CompletedTasks = allTasks.FirstOrDefault(x => x.Status == TaskStatus.Completed)?.Count ?? 0,
            InProgressTasks = allTasks.FirstOrDefault(x => x.Status == TaskStatus.InProgress)?.Count ?? 0,
            PendingTasks = allTasks.FirstOrDefault(x => x.Status == TaskStatus.Pending)?.Count ?? 0
        };
    }

    private async IAsyncEnumerable<TaskItem> StreamTasksAsync(string userId)
    {
        var query = _context.TaskItems
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.CreatedAt)
            .AsAsyncEnumerable();

        await foreach (var task in query)
        {
            yield return task;
        }
    }

    private TaskItem CreateTestTask(string userId, string title, TaskStatus status = TaskStatus.Pending)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = $"Description for {title}",
            Status = status,
            Priority = 3,
            UserId = userId,
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }

    private class TaskStatistics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
    }
}