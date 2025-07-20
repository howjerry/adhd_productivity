using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdhdProductivitySystem.Tests.Unit.Infrastructure;

public class UnitOfWorkTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Mock<ILogger<UnitOfWork>> _mockLogger;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<UnitOfWork>>();
        _unitOfWork = new UnitOfWork(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnNumberOfAffectedEntries()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Status = TaskStatus.Todo,
            Priority = Priority.Medium,
            UserId = user.Id,
            Tags = string.Empty,
            Notes = string.Empty
        };

        // Act
        _unitOfWork.Repository<User>().Add(user);
        _unitOfWork.Tasks.Add(task);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(2, result); // 2 entities were saved
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldStartTransaction()
    {
        // Act
        var transaction = await _unitOfWork.BeginTransactionAsync();

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(transaction, _unitOfWork.CurrentTransaction);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionAlreadyExists_ShouldThrowException()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldCommitOnSuccess()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };

        // Act
        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _unitOfWork.Repository<User>().Add(user);
            return user.Id;
        });

        // Assert
        Assert.Equal(user.Id, result);
        Assert.Null(_unitOfWork.CurrentTransaction); // Transaction should be completed
        
        // Verify user was saved
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(user.Email, savedUser.Email);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OnException_ShouldRollback()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                _unitOfWork.Repository<User>().Add(user);
                throw new InvalidOperationException("Test exception");
            });
        });

        // Assert
        Assert.Null(_unitOfWork.CurrentTransaction); // Transaction should be rolled back
        
        // Verify user was not saved
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.Null(savedUser);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithoutResult_ShouldCommitOnSuccess()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };

        // Act
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _unitOfWork.Repository<User>().Add(user);
            await Task.CompletedTask;
        });

        // Assert
        Assert.Null(_unitOfWork.CurrentTransaction); // Transaction should be completed
        
        // Verify user was saved
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public void HasUnsavedChanges_WhenNoChanges_ShouldReturnFalse()
    {
        // Act & Assert
        Assert.False(_unitOfWork.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_WhenChangesExist_ShouldReturnTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };

        // Act
        _unitOfWork.Repository<User>().Add(user);

        // Assert
        Assert.True(_unitOfWork.HasUnsavedChanges);
    }

    [Fact]
    public void Repository_ShouldReturnSameInstanceForSameType()
    {
        // Act
        var repo1 = _unitOfWork.Repository<User>();
        var repo2 = _unitOfWork.Repository<User>();

        // Assert
        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void Tasks_ShouldReturnTaskRepository()
    {
        // Act
        var taskRepo = _unitOfWork.Tasks;

        // Assert
        Assert.NotNull(taskRepo);
        Assert.IsAssignableFrom<ITaskRepository>(taskRepo);
    }

    [Fact]
    public async Task ComplexTransaction_ShouldMaintainDataConsistency()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };

        var task1 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task 1",
            Status = TaskStatus.Todo,
            Priority = Priority.High,
            UserId = user.Id,
            Tags = string.Empty,
            Notes = string.Empty
        };

        var task2 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task 2",
            Status = TaskStatus.InProgress,
            Priority = Priority.Medium,
            UserId = user.Id,
            Tags = string.Empty,
            Notes = string.Empty
        };

        // Act
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 添加用戶
            _unitOfWork.Repository<User>().Add(user);
            
            // 添加任務
            _unitOfWork.Tasks.Add(task1);
            _unitOfWork.Tasks.Add(task2);
            
            // 模擬一些業務邏輯
            task1.EstimatedMinutes = 60;
            task2.EstimatedMinutes = 120;
            
            await Task.CompletedTask;
        });

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        var savedTask1 = await _context.Tasks.FindAsync(task1.Id);
        var savedTask2 = await _context.Tasks.FindAsync(task2.Id);

        Assert.NotNull(savedUser);
        Assert.NotNull(savedTask1);
        Assert.NotNull(savedTask2);
        Assert.Equal(60, savedTask1.EstimatedMinutes);
        Assert.Equal(120, savedTask2.EstimatedMinutes);
        Assert.Equal(user.Id, savedTask1.UserId);
        Assert.Equal(user.Id, savedTask2.UserId);
    }

    public void Dispose()
    {
        _unitOfWork?.Dispose();
        _context?.Dispose();
    }
}