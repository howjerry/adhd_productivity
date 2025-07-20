using System;
using System.Linq;
using System.Threading.Tasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskStatus = AdhdProductivitySystem.Domain.Enums.TaskStatus;

namespace AdhdProductivitySystem.Infrastructure.Tests.Data;

public class TaskRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskRepository _repository;

    public TaskRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new TaskRepository(_context);
    }

    [Fact]
    public async Task GetTaskWithSubtasksAsync_Should_Include_Subtasks()
    {
        // Arrange
        var parentTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Parent Task",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subtask1 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Subtask 1",
            UserId = Guid.NewGuid(),
            ParentTaskId = parentTask.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subtask2 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Subtask 2",
            UserId = Guid.NewGuid(),
            ParentTaskId = parentTask.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Tasks.AddRangeAsync(parentTask, subtask1, subtask2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTaskWithSubtasksAsync(parentTask.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Subtasks.Count);
        Assert.Contains(result.Subtasks, s => s.Title == "Subtask 1");
        Assert.Contains(result.Subtasks, s => s.Title == "Subtask 2");
    }

    [Fact]
    public async Task GetUserTasksWithSubtasksAsync_Should_Return_Only_Top_Level_Tasks()
    {
        // Arrange
        var userId = "test-user";
        var parentTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Parent Task",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subtask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Subtask",
            UserId = userId,
            ParentTaskId = parentTask.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Tasks.AddRangeAsync(parentTask, subtask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserTasksWithSubtasksAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Parent Task", result[0].Title);
        Assert.Single(result[0].Subtasks);
    }

    [Fact]
    public async Task GetOverdueTasksAsync_Should_Return_Past_Due_Tasks()
    {
        // Arrange
        var userId = "test-user";
        var now = DateTime.UtcNow;

        var overdueTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Overdue Task",
            UserId = userId,
            DueDate = now.AddDays(-1),
            Status = TaskStatus.InProgress,
            CreatedAt = now,
            UpdatedAt = now
        };

        var futureTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Future Task",
            UserId = userId,
            DueDate = now.AddDays(1),
            Status = TaskStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        var completedTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Completed Task",
            UserId = userId,
            DueDate = now.AddDays(-2),
            Status = TaskStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.Tasks.AddRangeAsync(overdueTask, futureTask, completedTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOverdueTasksAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Overdue Task", result[0].Title);
    }

    [Fact]
    public async Task GetTodayTasksAsync_Should_Return_Tasks_Due_Today()
    {
        // Arrange
        var userId = "test-user";
        var today = DateTime.UtcNow.Date;

        var todayTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Today Task",
            UserId = userId,
            DueDate = today.AddHours(14),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tomorrowTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Tomorrow Task",
            UserId = userId,
            DueDate = today.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Tasks.AddRangeAsync(todayTask, tomorrowTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTodayTasksAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Today Task", result[0].Title);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_Should_Update_Status_And_CompletedAt()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task to Complete",
            UserId = Guid.NewGuid(),
            Status = TaskStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();

        // Act
        var originalUpdatedAt = task.UpdatedAt;
        await Task.Delay(10); // 確保時間有變化
        await _repository.UpdateTaskStatusAsync(task.Id, TaskStatus.Completed);

        // Assert
        var updatedTask = await _context.Tasks.FindAsync(task.Id);
        Assert.Equal(TaskStatus.Completed, updatedTask!.Status);
        Assert.NotNull(updatedTask.CompletedAt);
        Assert.True(updatedTask.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UserOwnsTaskAsync_Should_Return_True_For_Owned_Task()
    {
        // Arrange
        var userId = "test-user";
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "User Task",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();

        // Act
        var ownsTask = await _repository.UserOwnsTaskAsync(task.Id, userId);
        var doesNotOwnTask = await _repository.UserOwnsTaskAsync(task.Id, "other-user");

        // Assert
        Assert.True(ownsTask);
        Assert.False(doesNotOwnTask);
    }

    [Fact]
    public async Task GetTasksNeedingRemindersAsync_Should_Return_Tasks_With_Due_Reminders()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var taskNeedingReminder = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task Needing Reminder",
            UserId = Guid.NewGuid(),
            ReminderTime = now.AddMinutes(-5),
            IsReminderSent = false,
            Status = TaskStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        var taskAlreadyReminded = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task Already Reminded",
            UserId = Guid.NewGuid(),
            ReminderTime = now.AddMinutes(-10),
            IsReminderSent = true,
            Status = TaskStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        var futureReminder = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Future Reminder",
            UserId = Guid.NewGuid(),
            ReminderTime = now.AddMinutes(10),
            IsReminderSent = false,
            Status = TaskStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.Tasks.AddRangeAsync(taskNeedingReminder, taskAlreadyReminded, futureReminder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTasksNeedingRemindersAsync(now);

        // Assert
        Assert.Single(result);
        Assert.Equal("Task Needing Reminder", result[0].Title);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}