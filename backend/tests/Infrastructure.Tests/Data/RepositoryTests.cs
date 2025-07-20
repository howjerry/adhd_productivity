using System;
using System.Linq;
using System.Threading.Tasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdhdProductivitySystem.Infrastructure.Tests.Data;

public class RepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<TaskItem> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<TaskItem>(_context);
    }

    [Fact]
    public async Task AddAsync_Should_Add_Entity()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(task);
        await _context.SaveChangesAsync();

        // Assert
        var savedTask = await _repository.GetByIdAsync(task.Id);
        Assert.NotNull(savedTask);
        Assert.Equal(task.Title, savedTask.Title);
    }

    [Fact]
    public async Task FindAsync_Should_Return_Filtered_Results()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tasks = new[]
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 3", UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _repository.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var userTasks = await _repository.GetListAsync(t => t.UserId == userId);

        // Assert
        Assert.Equal(2, userTasks.Count);
        Assert.All(userTasks, t => Assert.Equal(userId, t.UserId));
    }

    [Fact]
    public async Task Update_Should_Modify_Entity()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(task);
        await _context.SaveChangesAsync();

        // Act
        task.Title = "Updated Title";
        _repository.Update(task);
        await _context.SaveChangesAsync();

        // Assert
        var updatedTask = await _repository.GetByIdAsync(task.Id);
        Assert.Equal("Updated Title", updatedTask?.Title);
    }

    [Fact]
    public async Task Remove_Should_Delete_Entity()
    {
        // Arrange
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task to Delete",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(task);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(task);
        await _context.SaveChangesAsync();

        // Assert
        var deletedTask = await _repository.GetByIdAsync(task.Id);
        Assert.Null(deletedTask);
    }

    [Fact]
    public async Task Query_Should_Return_IQueryable()
    {
        // Arrange
        var tasks = new[]
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", Priority = AdhdProductivitySystem.Domain.Enums.Priority.High, UserId = "test-user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", Priority = AdhdProductivitySystem.Domain.Enums.Priority.Low, UserId = "test-user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 3", Priority = AdhdProductivitySystem.Domain.Enums.Priority.Medium, UserId = "test-user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _repository.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var highPriorityTasks = await _repository.Query
            .Where(t => t.Priority == AdhdProductivitySystem.Domain.Enums.Priority.High)
            .ToListAsync();

        // Assert
        Assert.Single(highPriorityTasks);
        Assert.Equal("Task 1", highPriorityTasks[0].Title);
    }

    [Fact]
    public async Task CountAsync_Should_Return_Correct_Count()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tasks = new[]
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 3", UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _repository.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var totalCount = await _repository.CountAsync();
        var userCount = await _repository.CountAsync(t => t.UserId == userId);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, userCount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}