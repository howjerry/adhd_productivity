using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AdhdProductivitySystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace AdhdProductivitySystem.Tests.Unit.Application.Handlers.Tasks;

/// <summary>
/// 測試優化後的 GetTasksQueryHandler 效能
/// </summary>
public class GetTasksQueryHandlerOptimizedTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly List<string> _sqlQueries;
    private readonly Guid _userId = Guid.NewGuid();

    public GetTasksQueryHandlerOptimizedTests(ITestOutputHelper output)
    {
        _output = output;
        _sqlQueries = new List<string>();

        // 設定 InMemory 資料庫與 SQL 查詢追蹤
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .LogTo(sql => 
            {
                _sqlQueries.Add(sql);
                _output.WriteLine($"SQL: {sql}");
            }, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        _context = new ApplicationDbContext(options);

        // 設定 Mock 服務
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
    }

    /// <summary>
    /// 測試：驗證優化後只產生單一查詢
    /// </summary>
    [Fact]
    public async Task Handle_WithProjection_GeneratesSingleQuery()
    {
        // Arrange
        var user = new User
        {
            Id = _userId,
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };
        _context.Users.Add(user);

        // 建立主任務與子任務
        const int taskCount = 10;
        const int subTasksPerTask = 5;
        
        for (int i = 0; i < taskCount; i++)
        {
            var mainTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = $"Task {i + 1}",
                Description = $"Description for task {i + 1}",
                Status = Domain.Enums.TaskStatus.InProgress,
                Priority = Priority.Medium,
                UserId = _userId,
                User = user,
                SubTasks = new List<TaskItem>()
            };

            // 添加子任務
            for (int j = 0; j < subTasksPerTask; j++)
            {
                var subTask = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = $"SubTask {i + 1}-{j + 1}",
                    Status = j < 2 ? Domain.Enums.TaskStatus.Completed : Domain.Enums.TaskStatus.InProgress,
                    Priority = Priority.Low,
                    UserId = _userId,
                    User = user,
                    ParentTaskId = mainTask.Id,
                    ParentTask = mainTask
                };
                mainTask.SubTasks.Add(subTask);
                _context.Tasks.Add(subTask);
            }

            _context.Tasks.Add(mainTask);
        }

        await _context.SaveChangesAsync();
        _sqlQueries.Clear(); // 清除設定時的查詢

        // Act
        var handler = new GetTasksQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetTasksQuery
        {
            Page = 1,
            PageSize = 10,
            SortBy = "createdAt",
            SortDescending = false
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"執行時間: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"SQL 查詢數量: {_sqlQueries.Count}");
        
        result.Should().HaveCount(taskCount);
        
        // 驗證每個任務的子任務計數是否正確
        foreach (var taskDto in result)
        {
            taskDto.SubTaskCount.Should().Be(subTasksPerTask);
            taskDto.CompletedSubTaskCount.Should().Be(2);
        }

        // 分析 SQL 查詢 - 應該只有一個主查詢
        var mainQueries = _sqlQueries.Count(q => q.Contains("SELECT") && !q.Contains("INSERT"));
        
        _output.WriteLine($"SELECT 查詢數量: {mainQueries}");
        
        // 優化後應該只有 1 個 SELECT 查詢
        mainQueries.Should().Be(1, "應該只產生單一查詢");
        
        // 記錄查詢以便調試
        _output.WriteLine("\n產生的 SQL 查詢:");
        foreach (var sql in _sqlQueries.Where(q => q.Contains("SELECT")))
        {
            _output.WriteLine(sql);
        }
    }

    /// <summary>
    /// 測試：比較優化前後的效能差異
    /// </summary>
    [Fact]
    public async Task Handle_PerformanceComparison_ShowsImprovement()
    {
        // Arrange
        var user = new User
        {
            Id = _userId,
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };
        _context.Users.Add(user);

        // 建立大量測試資料
        const int taskCount = 50;
        const int subTasksPerTask = 10;
        
        for (int i = 0; i < taskCount; i++)
        {
            var mainTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = $"Performance Task {i + 1}",
                Description = $"Testing performance with task {i + 1}",
                Status = (Domain.Enums.TaskStatus)(i % 4),
                Priority = (Priority)(i % 3),
                UserId = _userId,
                User = user,
                DueDate = DateTime.UtcNow.AddDays(i % 30 - 15),
                Tags = $"perf,test,tag{i % 5}",
                EstimatedMinutes = (i + 1) * 15,
                ActualMinutes = (i + 1) * 10,
                SubTasks = new List<TaskItem>()
            };

            // 添加子任務
            for (int j = 0; j < subTasksPerTask; j++)
            {
                var subTask = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = $"SubTask {i + 1}-{j + 1}",
                    Status = j % 3 == 0 ? Domain.Enums.TaskStatus.Completed : Domain.Enums.TaskStatus.InProgress,
                    Priority = Priority.Low,
                    UserId = _userId,
                    User = user,
                    ParentTaskId = mainTask.Id,
                    ParentTask = mainTask
                };
                mainTask.SubTasks.Add(subTask);
                _context.Tasks.Add(subTask);
            }

            _context.Tasks.Add(mainTask);
        }

        await _context.SaveChangesAsync();
        _sqlQueries.Clear();

        // Act - 測試複雜查詢
        var handler = new GetTasksQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetTasksQuery
        {
            Page = 1,
            PageSize = 20,
            Status = Domain.Enums.TaskStatus.InProgress,
            Priority = Priority.High,
            Tags = "perf,test",
            SortBy = "dueDate",
            SortDescending = false
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"總任務數: {taskCount}");
        _output.WriteLine($"每個任務的子任務數: {subTasksPerTask}");
        _output.WriteLine($"執行時間: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"SQL 查詢數量: {_sqlQueries.Count}");
        _output.WriteLine($"返回結果數量: {result.Count}");

        // 效能基準
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "優化後的查詢應該在 500ms 內完成");
        
        // 查詢數量驗證
        var selectQueries = _sqlQueries.Count(q => q.Contains("SELECT"));
        selectQueries.Should().Be(1, "應該只有一個 SELECT 查詢");
        
        // 結果驗證
        result.All(t => t.Status == Domain.Enums.TaskStatus.InProgress).Should().BeTrue();
        result.All(t => t.Priority == Priority.High).Should().BeTrue();
        result.All(t => t.Tags.Contains("perf") && t.Tags.Contains("test")).Should().BeTrue();
    }

    /// <summary>
    /// 測試：驗證複合索引的效果
    /// </summary>
    [Fact]
    public async Task Handle_WithCompositeIndexFilters_PerformsEfficiently()
    {
        // Arrange
        var user = new User
        {
            Id = _userId,
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };
        _context.Users.Add(user);

        // 建立大量任務以測試索引效果
        const int taskCount = 200;
        
        for (int i = 0; i < taskCount; i++)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = $"Index Test Task {i + 1}",
                Status = (Domain.Enums.TaskStatus)(i % 4),
                Priority = (Priority)(i % 3),
                UserId = _userId,
                User = user,
                DueDate = DateTime.UtcNow.AddDays(i % 60 - 30),
                SubTasks = new List<TaskItem>()
            };

            // 為部分任務添加子任務
            if (i % 3 == 0)
            {
                for (int j = 0; j < 3; j++)
                {
                    var subTask = new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Title = $"SubTask {i + 1}-{j + 1}",
                        Status = Domain.Enums.TaskStatus.Todo,
                        Priority = Priority.Low,
                        UserId = _userId,
                        User = user,
                        ParentTaskId = task.Id,
                        ParentTask = task
                    };
                    task.SubTasks.Add(subTask);
                    _context.Tasks.Add(subTask);
                }
            }

            _context.Tasks.Add(task);
        }

        await _context.SaveChangesAsync();
        _sqlQueries.Clear();

        // Act - 使用複合索引的查詢
        var handler = new GetTasksQueryHandler(_context, _currentUserServiceMock.Object);
        
        // 測試 UserId + Status + Priority 複合索引
        var query1 = new GetTasksQuery
        {
            Page = 1,
            PageSize = 10,
            Status = Domain.Enums.TaskStatus.InProgress,
            Priority = Priority.High,
            SortBy = "createdAt",
            SortDescending = true
        };

        var stopwatch1 = Stopwatch.StartNew();
        var result1 = await handler.Handle(query1, CancellationToken.None);
        stopwatch1.Stop();

        _sqlQueries.Clear();

        // 測試 UserId + DueDate 複合索引
        var query2 = new GetTasksQuery
        {
            Page = 1,
            PageSize = 10,
            DueDateFrom = DateTime.UtcNow.AddDays(-7),
            DueDateTo = DateTime.UtcNow.AddDays(7),
            SortBy = "dueDate",
            SortDescending = false
        };

        var stopwatch2 = Stopwatch.StartNew();
        var result2 = await handler.Handle(query2, CancellationToken.None);
        stopwatch2.Stop();

        // Assert
        _output.WriteLine("=== 測試 UserId + Status + Priority 索引 ===");
        _output.WriteLine($"執行時間: {stopwatch1.ElapsedMilliseconds}ms");
        _output.WriteLine($"結果數量: {result1.Count}");
        
        _output.WriteLine("\n=== 測試 UserId + DueDate 索引 ===");
        _output.WriteLine($"執行時間: {stopwatch2.ElapsedMilliseconds}ms");
        _output.WriteLine($"結果數量: {result2.Count}");

        // 兩個查詢都應該很快完成
        stopwatch1.ElapsedMilliseconds.Should().BeLessThan(200);
        stopwatch2.ElapsedMilliseconds.Should().BeLessThan(200);
        
        // 驗證結果
        result1.All(t => t.Status == Domain.Enums.TaskStatus.InProgress && 
                        t.Priority == Priority.High).Should().BeTrue();
        
        result2.All(t => t.DueDate >= DateTime.UtcNow.AddDays(-7) && 
                        t.DueDate <= DateTime.UtcNow.AddDays(7)).Should().BeTrue();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}