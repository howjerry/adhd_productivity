using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace AdhdProductivitySystem.Tests.Application.Handlers.Tasks;

public class GetTasksQueryHandlerCacheTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<GetTasksQueryHandler>> _mockLogger;
    private readonly Mock<DbSet<TaskItem>> _mockTaskSet;
    private readonly GetTasksQueryHandler _handler;

    public GetTasksQueryHandlerCacheTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<GetTasksQueryHandler>>();
        _mockTaskSet = new Mock<DbSet<TaskItem>>();

        _mockContext.Setup(c => c.Tasks).Returns(_mockTaskSet.Object);

        _handler = new GetTasksQueryHandler(
            _mockContext.Object,
            _mockCurrentUserService.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var query = new GetTasksQuery();
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsFromCacheWithoutDatabaseCall()
    {
        // Arrange
        var userId = 123;
        var query = new GetTasksQuery { Page = 1, PageSize = 10 };
        var cachedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = 1,
                Title = "Cached Task",
                UserId = userId,
                Status = Domain.Enums.TaskStatus.InProgress,
                Priority = Priority.Medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        _mockCacheService
            .Setup(c => c.GetOrSetAsync<List<TaskDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<TaskDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTasks);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(cachedTasks, result);
        Assert.Single(result);
        Assert.Equal("Cached Task", result[0].Title);

        // Verify cache was called with correct parameters
        _mockCacheService.Verify(
            c => c.GetOrSetAsync<List<TaskDto>>(
                It.Is<string>(key => key.Contains($"tasks:user:{userId}:query:")),
                It.IsAny<Func<Task<List<TaskDto>>>>(),
                TimeSpan.FromMinutes(5),
                It.Is<string[]>(tags => tags.Contains($"user:{userId}") && tags.Contains("tasks")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GeneratesCacheKeyWithQueryParameters()
    {
        // Arrange
        var userId = 123;
        var query = new GetTasksQuery
        {
            Status = Domain.Enums.TaskStatus.Completed,
            Priority = Priority.High,
            SearchText = "important",
            Page = 2,
            PageSize = 20,
            SortBy = "priority",
            SortDescending = true
        };

        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var capturedCacheKey = string.Empty;
        _mockCacheService
            .Setup(c => c.GetOrSetAsync<List<TaskDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<TaskDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<List<TaskDto>>>, TimeSpan?, string[], CancellationToken>(
                (key, factory, expiry, tags, ct) => capturedCacheKey = key)
            .ReturnsAsync(new List<TaskDto>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Contains($"tasks:user:{userId}:query:", capturedCacheKey);
        
        _mockCacheService.Verify(
            c => c.GetOrSetAsync<List<TaskDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<TaskDto>>>>(),
                TimeSpan.FromMinutes(5),
                It.Is<string[]>(tags => tags.Contains($"user:{userId}") && tags.Contains("tasks")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentQueriesGenerateDifferentCacheKeys()
    {
        // Arrange
        var userId = 123;
        var query1 = new GetTasksQuery { Page = 1, PageSize = 10, Status = Domain.Enums.TaskStatus.InProgress };
        var query2 = new GetTasksQuery { Page = 1, PageSize = 10, Status = Domain.Enums.TaskStatus.Completed };

        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var capturedKeys = new List<string>();
        _mockCacheService
            .Setup(c => c.GetOrSetAsync<List<TaskDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<TaskDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<List<TaskDto>>>, TimeSpan?, string[], CancellationToken>(
                (key, factory, expiry, tags, ct) => capturedKeys.Add(key))
            .ReturnsAsync(new List<TaskDto>());

        // Act
        await _handler.Handle(query1, CancellationToken.None);
        await _handler.Handle(query2, CancellationToken.None);

        // Assert
        Assert.Equal(2, capturedKeys.Count);
        Assert.NotEqual(capturedKeys[0], capturedKeys[1]);
        Assert.All(capturedKeys, key => Assert.Contains($"tasks:user:{userId}:query:", key));
    }

    [Fact]
    public async Task Handle_LogsTaskCountCorrectly()
    {
        // Arrange
        var userId = 123;
        var query = new GetTasksQuery();
        var tasks = new List<TaskDto>
        {
            new TaskDto { Id = 1, Title = "Task 1", UserId = userId },
            new TaskDto { Id = 2, Title = "Task 2", UserId = userId },
            new TaskDto { Id = 3, Title = "Task 3", UserId = userId }
        };

        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        _mockCacheService
            .Setup(c => c.GetOrSetAsync<List<TaskDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<TaskDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved 3 tasks for user 123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}