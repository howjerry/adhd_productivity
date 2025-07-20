using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AdhdProductivitySystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AdhdProductivitySystem.Tests.Unit.Application.Handlers.Tasks;

/// <summary>
/// 簡單的單元測試驗證 GetTasksQueryHandler 功能
/// </summary>
public class GetTasksQueryHandlerUnitTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Guid _userId = Guid.NewGuid();

    public GetTasksQueryHandlerUnitTest()
    {
        // 設定 InMemory 資料庫
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        // 設定 Mock 服務
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_ReturnsTasksWithCorrectSubTaskCounts()
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

        // 建立任務與子任務
        var task1 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Main Task 1",
            Status = Domain.Enums.TaskStatus.InProgress,
            Priority = Priority.High,
            UserId = _userId,
            User = user,
            SubTasks = new List<TaskItem>
            {
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "SubTask 1-1",
                    Status = Domain.Enums.TaskStatus.Completed,
                    Priority = Priority.Low,
                    UserId = _userId,
                    User = user,
                    ParentTaskId = Guid.NewGuid()
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "SubTask 1-2",
                    Status = Domain.Enums.TaskStatus.InProgress,
                    Priority = Priority.Low,
                    UserId = _userId,
                    User = user,
                    ParentTaskId = Guid.NewGuid()
                }
            }
        };

        var task2 = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Main Task 2",
            Status = Domain.Enums.TaskStatus.Todo,
            Priority = Priority.Medium,
            UserId = _userId,
            User = user,
            SubTasks = new List<TaskItem>()
        };

        _context.Tasks.Add(task1);
        _context.Tasks.Add(task2);
        await _context.SaveChangesAsync();

        // Act
        var handler = new GetTasksQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetTasksQuery
        {
            Page = 1,
            PageSize = 10,
            SortBy = "createdAt",
            SortDescending = false
        };

        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        
        var task1Dto = result.First(t => t.Title == "Main Task 1");
        task1Dto.SubTaskCount.Should().Be(2);
        task1Dto.CompletedSubTaskCount.Should().Be(1);
        
        var task2Dto = result.First(t => t.Title == "Main Task 2");
        task2Dto.SubTaskCount.Should().Be(0);
        task2Dto.CompletedSubTaskCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithFilters_ReturnsFilteredResults()
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

        // 建立不同狀態和優先級的任務
        var tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "High Priority InProgress Task",
                Status = Domain.Enums.TaskStatus.InProgress,
                Priority = Priority.High,
                UserId = _userId,
                User = user,
                SubTasks = new List<TaskItem>()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Low Priority InProgress Task",
                Status = Domain.Enums.TaskStatus.InProgress,
                Priority = Priority.Low,
                UserId = _userId,
                User = user,
                SubTasks = new List<TaskItem>()
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "High Priority Completed Task",
                Status = Domain.Enums.TaskStatus.Completed,
                Priority = Priority.High,
                UserId = _userId,
                User = user,
                SubTasks = new List<TaskItem>()
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var handler = new GetTasksQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetTasksQuery
        {
            Page = 1,
            PageSize = 10,
            Status = Domain.Enums.TaskStatus.InProgress,
            Priority = Priority.High,
            SortBy = "title",
            SortDescending = false
        };

        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("High Priority InProgress Task");
        result.First().Status.Should().Be(Domain.Enums.TaskStatus.InProgress);
        result.First().Priority.Should().Be(Priority.High);
    }

    [Fact]
    public async Task Handle_WithoutAuthentication_ThrowsUnauthorizedException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var handler = new GetTasksQueryHandler(_context, _currentUserServiceMock.Object);
        var query = new GetTasksQuery
        {
            Page = 1,
            PageSize = 10,
            SortBy = "createdAt",
            SortDescending = false
        };

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            handler.Handle(query, CancellationToken.None));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}