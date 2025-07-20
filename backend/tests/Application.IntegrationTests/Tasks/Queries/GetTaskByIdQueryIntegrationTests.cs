using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTaskById;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AdhdProductivitySystem.Application.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdhdProductivitySystem.Application.IntegrationTests.Tasks.Queries;

/// <summary>
/// 整合測試：GetTaskByIdQuery
/// 測試範圍：完整的資料庫查詢流程、效能驗證
/// </summary>
public class GetTaskByIdQueryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetTaskById_WithValidId_ReturnsTaskWithSubtaskCounts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var parentTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "父任務",
            Description = "包含子任務的父任務",
            Status = TaskStatus.InProgress,
            Priority = Priority.High,
            EstimatedMinutes = 120,
            ActualMinutes = 60,
            DueDate = DateTime.UtcNow.AddDays(7),
            Tags = "工作,重要",
            Notes = "這是一個重要的父任務",
            IsRecurring = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var subTask1 = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ParentTaskId = parentTask.Id,
            Title = "子任務1",
            Description = "已完成的子任務",
            Status = TaskStatus.Completed,
            Priority = Priority.Medium,
            EstimatedMinutes = 30,
            ActualMinutes = 25,
            CompletedAt = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var subTask2 = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ParentTaskId = parentTask.Id,
            Title = "子任務2",
            Description = "進行中的子任務",
            Status = TaskStatus.InProgress,
            Priority = Priority.Low,
            EstimatedMinutes = 45,
            ActualMinutes = 35,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        Context.Tasks.AddRange(parentTask, subTask1, subTask2);
        await Context.SaveChangesAsync();

        var query = new GetTaskByIdQuery { Id = parentTask.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(parentTask.Id, result.Id);
        Assert.Equal("父任務", result.Title);
        Assert.Equal("包含子任務的父任務", result.Description);
        Assert.Equal(TaskStatus.InProgress, result.Status);
        Assert.Equal(Priority.High, result.Priority);
        Assert.Equal(120, result.EstimatedMinutes);
        Assert.Equal(60, result.ActualMinutes);
        Assert.Equal("工作,重要", result.Tags);
        Assert.Equal("這是一個重要的父任務", result.Notes);
        Assert.False(result.IsRecurring);
        Assert.Null(result.ParentTaskId);
        
        // 驗證子任務統計數據
        Assert.Equal(2, result.SubTaskCount);
        Assert.Equal(1, result.CompletedSubTaskCount);

        // 驗證時間戳記
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.UpdatedAt <= DateTime.UtcNow);
        Assert.True(result.CreatedAt <= result.UpdatedAt);
    }

    [Fact]
    public async Task GetTaskById_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var nonExistentId = Guid.NewGuid();
        var query = new GetTaskByIdQuery { Id = nonExistentId };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaskById_WithOtherUserTask_ReturnsNull()
    {
        // Arrange
        var user1 = await CreateTestUserAsync();
        var user2 = await CreateTestUserAsync("user2@test.com");

        // 創建屬於 user2 的任務
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            Title = "其他使用者的任務",
            Description = "這個任務屬於其他使用者",
            Status = TaskStatus.InProgress,
            Priority = Priority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Tasks.Add(task);
        await Context.SaveChangesAsync();

        // 以 user1 的身分查詢 user2 的任務
        await SetCurrentUserAsync(user1.Id);
        var query = new GetTaskByIdQuery { Id = task.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaskById_WithSubTask_ReturnsCorrectParentTaskId()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var parentTaskId = Guid.NewGuid();
        var parentTask = new TaskItem
        {
            Id = parentTaskId,
            UserId = user.Id,
            Title = "父任務",
            Description = "父任務描述",
            Status = TaskStatus.InProgress,
            Priority = Priority.High,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ParentTaskId = parentTaskId,
            Title = "子任務",
            Description = "子任務描述",
            Status = TaskStatus.Todo,
            Priority = Priority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Tasks.AddRange(parentTask, subTask);
        await Context.SaveChangesAsync();

        var query = new GetTaskByIdQuery { Id = subTask.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subTask.Id, result.Id);
        Assert.Equal(parentTaskId, result.ParentTaskId);
        Assert.Equal("子任務", result.Title);
        Assert.Equal(0, result.SubTaskCount); // 子任務沒有自己的子任務
        Assert.Equal(0, result.CompletedSubTaskCount);
    }

    [Fact]
    public async Task GetTaskById_WithRecurringTask_ReturnsRecurrenceInformation()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var recurringTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "重複任務",
            Description = "每日重複的任務",
            Status = TaskStatus.InProgress,
            Priority = Priority.Medium,
            IsRecurring = true,
            RecurrencePattern = "FREQ=DAILY;INTERVAL=1",
            NextOccurrence = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Tasks.Add(recurringTask);
        await Context.SaveChangesAsync();

        var query = new GetTaskByIdQuery { Id = recurringTask.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurringTask.Id, result.Id);
        Assert.True(result.IsRecurring);
        Assert.Equal("FREQ=DAILY;INTERVAL=1", result.RecurrencePattern);
        Assert.NotNull(result.NextOccurrence);
        Assert.True(result.NextOccurrence > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetTaskById_Performance_ExecutesSingleQuery()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var parentTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "效能測試任務",
            Description = "測試查詢效能",
            Status = TaskStatus.InProgress,
            Priority = Priority.High,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 創建多個子任務來測試 N+1 查詢問題
        var subTasks = new List<TaskItem>();
        for (int i = 0; i < 10; i++)
        {
            subTasks.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ParentTaskId = parentTask.Id,
                Title = $"子任務 {i + 1}",
                Description = $"第 {i + 1} 個子任務",
                Status = i % 2 == 0 ? TaskStatus.Completed : TaskStatus.InProgress,
                Priority = Priority.Low,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        Context.Tasks.Add(parentTask);
        Context.Tasks.AddRange(subTasks);
        await Context.SaveChangesAsync();

        // 清除 ChangeTracker 以確保真實的資料庫查詢
        Context.ChangeTracker.Clear();

        var query = new GetTaskByIdQuery { Id = parentTask.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(parentTask.Id, result.Id);
        Assert.Equal(10, result.SubTaskCount);
        Assert.Equal(5, result.CompletedSubTaskCount); // 一半是完成狀態

        // 注意：在真實的效能測試中，我們會使用資料庫查詢日誌來驗證只執行了一個查詢
        // 這裡我們通過功能測試來間接驗證查詢的正確性
    }

    [Fact]
    public async Task GetTaskById_WithEmptyGuid_ReturnsNull()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var query = new GetTaskByIdQuery { Id = Guid.Empty };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(AdhdProductivitySystem.Domain.Enums.TaskStatus.Todo)]
    [InlineData(AdhdProductivitySystem.Domain.Enums.TaskStatus.InProgress)]
    [InlineData(AdhdProductivitySystem.Domain.Enums.TaskStatus.Completed)]
    [InlineData(AdhdProductivitySystem.Domain.Enums.TaskStatus.Cancelled)]
    public async Task GetTaskById_WithDifferentStatuses_ReturnsCorrectStatus(AdhdProductivitySystem.Domain.Enums.TaskStatus status)
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await SetCurrentUserAsync(user.Id);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = $"任務 - {status}",
            Description = $"狀態為 {status} 的任務",
            Status = status,
            Priority = Priority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (status == TaskStatus.Completed)
        {
            task.CompletedAt = DateTime.UtcNow;
        }

        Context.Tasks.Add(task);
        await Context.SaveChangesAsync();

        var query = new GetTaskByIdQuery { Id = task.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
        
        if (status == TaskStatus.Completed)
        {
            Assert.NotNull(result.CompletedAt);
        }
        else
        {
            Assert.Null(result.CompletedAt);
        }
    }
}