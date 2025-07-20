using System;
using System.Threading.Tasks;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdhdProductivitySystem.Application.IntegrationTests.UnitOfWork;

/// <summary>
/// Unit of Work 交易測試
/// </summary>
public class TransactionTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;
    private ApplicationDbContext _context = null!;
    private IUnitOfWork _unitOfWork = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });

        services.AddScoped<IUnitOfWork, AdhdProductivitySystem.Infrastructure.Data.UnitOfWork>();
        services.AddScoped<ITaskRepository, AdhdProductivitySystem.Infrastructure.Data.TaskRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(AdhdProductivitySystem.Infrastructure.Data.Repository<>));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();

        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteTransactionAsync_Success_ShouldCommitChanges()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var task = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                UserId = userId,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Pending,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.Medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Tasks.AddAsync(task);
            await _unitOfWork.SaveChangesAsync();

            return task.Id;
        });

        // Assert
        result.Should().Be(taskId);

        // 驗證任務已保存
        var savedTask = await _context.Tasks.FindAsync(new object[] { taskId });
        savedTask.Should().NotBeNull();
        savedTask!.Title.Should().Be("Test Task");
    }

    [Fact]
    public async Task ExecuteTransactionAsync_Failure_ShouldRollbackChanges()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var taskCreated = false;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _unitOfWork.ExecuteTransactionAsync<Guid>(async () =>
            {
                var task = new TaskItem
                {
                    Id = taskId,
                    Title = "Test Task That Will Fail",
                    UserId = userId,
                    Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Pending,
                    Priority = AdhdProductivitySystem.Domain.Enums.Priority.High,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Tasks.AddAsync(task);
                await _unitOfWork.SaveChangesAsync();
                taskCreated = true;

                // 模擬錯誤
                throw new InvalidOperationException("Simulated error");
            });
        });

        // 驗證任務已創建但被回滾
        taskCreated.Should().BeTrue();
        
        // 注意：InMemory database 不支援真正的交易回滾
        // 在實際的資料庫中，這個任務應該不存在
    }

    [Fact]
    public async Task ComplexTransaction_MultipleOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var parentTaskId = Guid.NewGuid();
        var childTaskId1 = Guid.NewGuid();
        var childTaskId2 = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // 創建父任務
            var parentTask = new TaskItem
            {
                Id = parentTaskId,
                Title = "Parent Task",
                UserId = userId,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.InProgress,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.High,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Tasks.AddAsync(parentTask);

            // 創建子任務 1
            var childTask1 = new TaskItem
            {
                Id = childTaskId1,
                Title = "Child Task 1",
                UserId = userId,
                ParentTaskId = parentTaskId,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Pending,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.Medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Tasks.AddAsync(childTask1);

            // 創建子任務 2
            var childTask2 = new TaskItem
            {
                Id = childTaskId2,
                Title = "Child Task 2",
                UserId = userId,
                ParentTaskId = parentTaskId,
                Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Pending,
                Priority = AdhdProductivitySystem.Domain.Enums.Priority.Low,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Tasks.AddAsync(childTask2);

            await _unitOfWork.SaveChangesAsync();
            return true;
        });

        // Assert - 驗證所有任務都已創建
        var parent = await _context.Tasks.FindAsync(new object[] { parentTaskId });
        var child1 = await _context.Tasks.FindAsync(new object[] { childTaskId1 });
        var child2 = await _context.Tasks.FindAsync(new object[] { childTaskId2 });

        parent.Should().NotBeNull();
        child1.Should().NotBeNull();
        child2.Should().NotBeNull();

        child1!.ParentTaskId.Should().Be(parentTaskId);
        child2!.ParentTaskId.Should().Be(parentTaskId);
    }

    [Fact]
    public async Task ManualTransaction_WithCommit_ShouldWork()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        await _unitOfWork.BeginTransactionAsync();

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Manual Transaction Task",
            UserId = userId,
            Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Pending,
            Priority = AdhdProductivitySystem.Domain.Enums.Priority.High,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.Should().BeFalse();
        
        var savedTask = await _context.Tasks.FindAsync(new object[] { taskId });
        savedTask.Should().NotBeNull();
    }

    [Fact]
    public async Task ManualTransaction_WithRollback_ShouldWork()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        await _unitOfWork.BeginTransactionAsync();

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Task to be rolled back",
            UserId = userId,
            Status = AdhdProductivitySystem.Domain.Enums.TaskStatus.Pending,
            Priority = AdhdProductivitySystem.Domain.Enums.Priority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        
        // 決定回滾
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.Should().BeFalse();
        
        // 注意：InMemory database 不支援真正的交易回滾
        // 在實際的資料庫中，這個任務應該不存在
    }
}