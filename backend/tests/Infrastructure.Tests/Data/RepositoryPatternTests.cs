using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using TaskStatus = AdhdProductivitySystem.Domain.Enums.TaskStatus;

namespace AdhdProductivitySystem.Infrastructure.Tests.Data;

/// <summary>
/// Repository Pattern 的完整單元測試
/// 測試通用 Repository 的所有基本功能
/// </summary>
public class RepositoryPatternTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<TaskItem> _repository;

    public RepositoryPatternTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<TaskItem>(_context);
    }

    #region 基本 CRUD 操作測試

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnEntity()
    {
        // Arrange
        var entity = CreateTestTask();
        await _context.Tasks.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetListAsync_WithNoData_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetListAsync_WithData_ShouldReturnAllEntities()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Task 1"),
            CreateTestTask("Task 2"),
            CreateTestTask("Task 3")
        };

        await _context.Tasks.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetListAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(entities, entity => 
            Assert.Contains(result, r => r.Id == entity.Id));
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_ShouldAddToContext()
    {
        // Arrange
        var entity = CreateTestTask();

        // Act
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var savedEntity = await _context.Tasks.FindAsync(entity.Id);
        Assert.NotNull(savedEntity);
        Assert.Equal(entity.Title, savedEntity.Title);
    }

    [Fact]
    public async Task AddRangeAsync_WithValidEntities_ShouldAddAllToContext()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Task 1"),
            CreateTestTask("Task 2")
        };

        // Act
        await _repository.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.Tasks.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void Update_WithValidEntity_ShouldUpdateInContext()
    {
        // Arrange
        var entity = CreateTestTask();
        _context.Tasks.Add(entity);
        _context.SaveChanges();

        var newTitle = "Updated Title";
        entity.Title = newTitle;

        // Act
        _repository.Update(entity);
        _context.SaveChanges();

        // Assert
        var updatedEntity = _context.Tasks.Find(entity.Id);
        Assert.NotNull(updatedEntity);
        Assert.Equal(newTitle, updatedEntity.Title);
    }

    [Fact]
    public void UpdateRange_WithValidEntities_ShouldUpdateAllInContext()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Task 1"),
            CreateTestTask("Task 2")
        };

        _context.Tasks.AddRange(entities);
        _context.SaveChanges();

        entities[0].Title = "Updated Task 1";
        entities[1].Title = "Updated Task 2";

        // Act
        _repository.UpdateRange(entities);
        _context.SaveChanges();

        // Assert
        var updatedEntities = _context.Tasks.ToList();
        Assert.Equal("Updated Task 1", updatedEntities[0].Title);
        Assert.Equal("Updated Task 2", updatedEntities[1].Title);
    }

    [Fact]
    public void Remove_WithValidEntity_ShouldRemoveFromContext()
    {
        // Arrange
        var entity = CreateTestTask();
        _context.Tasks.Add(entity);
        _context.SaveChanges();

        // Act
        _repository.Remove(entity);
        _context.SaveChanges();

        // Assert
        var removedEntity = _context.Tasks.Find(entity.Id);
        Assert.Null(removedEntity);
    }

    [Fact]
    public void RemoveRange_WithValidEntities_ShouldRemoveAllFromContext()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Task 1"),
            CreateTestTask("Task 2")
        };

        _context.Tasks.AddRange(entities);
        _context.SaveChanges();

        // Act
        _repository.RemoveRange(entities);
        _context.SaveChanges();

        // Assert
        var count = _context.Tasks.Count();
        Assert.Equal(0, count);
    }

    #endregion

    #region 查詢操作測試

    [Fact]
    public async Task FindAsync_WithValidPredicate_ShouldReturnMatchingEntities()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Important Task", TaskStatus.InProgress),
            CreateTestTask("Normal Task", TaskStatus.Pending),
            CreateTestTask("Another Important", TaskStatus.InProgress)
        };

        await _context.Tasks.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(t => t.Status == TaskStatus.InProgress);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(TaskStatus.InProgress, t.Status));
    }

    [Fact]
    public async Task FindAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var entity = CreateTestTask("Task", TaskStatus.Pending);
        await _context.Tasks.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(t => t.Status == TaskStatus.Completed);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindSingleAsync_WithValidPredicate_ShouldReturnFirstMatch()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("First", TaskStatus.InProgress),
            CreateTestTask("Second", TaskStatus.InProgress)
        };

        await _context.Tasks.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindSingleAsync(t => t.Status == TaskStatus.InProgress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task FindSingleAsync_WithNoMatches_ShouldReturnNull()
    {
        // Act
        var result = await _repository.FindSingleAsync(t => t.Status == TaskStatus.Completed);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WithMatchingData_ShouldReturnTrue()
    {
        // Arrange
        var entity = CreateTestTask("Task", TaskStatus.Pending);
        await _context.Tasks.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(t => t.Status == TaskStatus.Pending);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNoMatchingData_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(t => t.Status == TaskStatus.Completed);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CountAsync_WithValidPredicate_ShouldReturnCorrectCount()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Task 1", TaskStatus.Pending),
            CreateTestTask("Task 2", TaskStatus.Pending),
            CreateTestTask("Task 3", TaskStatus.Completed)
        };

        await _context.Tasks.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync(t => t.Status == TaskStatus.Pending);

        // Assert
        Assert.Equal(2, result);
    }

    #endregion

    #region Query 操作測試

    [Fact]
    public void Query_ShouldReturnQueryable()
    {
        // Act
        var query = _repository.Query();

        // Assert
        Assert.NotNull(query);
        Assert.IsAssignableFrom<IQueryable<TaskItem>>(query);
    }

    [Fact]
    public void QueryNoTracking_ShouldReturnQueryable()
    {
        // Act
        var query = _repository.QueryNoTracking();

        // Assert
        Assert.NotNull(query);
        Assert.IsAssignableFrom<IQueryable<TaskItem>>(query);
    }

    [Fact]
    public async Task QueryOperations_ShouldSupportComplexQueries()
    {
        // Arrange
        var entities = new List<TaskItem>
        {
            CreateTestTask("Important A", TaskStatus.Pending),
            CreateTestTask("Important B", TaskStatus.InProgress),
            CreateTestTask("Normal Task", TaskStatus.Completed)
        };

        await _context.Tasks.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act - 使用 Query 進行複雜查詢
        var result = await _repository.Query()
            .Where(t => t.Status != TaskStatus.Completed)
            .OrderBy(t => t.Title)
            .ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.NotEqual(TaskStatus.Completed, t.Status));
        Assert.Equal("Important A", result[0].Title);
        Assert.Equal("Important B", result[1].Title);
    }

    #endregion

    #region 私有輔助方法

    private TaskItem CreateTestTask(string title = "Test Task", TaskStatus status = TaskStatus.Todo)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test Description",
            Status = status,
            UserId = "test-user-id",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}