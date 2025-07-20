using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTaskById;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdhdProductivitySystem.Tests.Unit.Application.Handlers.Tasks;

/// <summary>
/// 單元測試：GetTaskByIdQueryHandler
/// 測試範圍：權限驗證、錯誤處理、查詢效能
/// </summary>
public class GetTaskByIdQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GetTaskByIdQueryHandler>> _mockLogger;
    private readonly GetTaskByIdQueryHandler _handler;
    private readonly Mock<DbSet<TaskItem>> _mockTaskSet;

    public GetTaskByIdQueryHandlerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GetTaskByIdQueryHandler>>();
        _mockTaskSet = new Mock<DbSet<TaskItem>>();

        _mockContext.Setup(x => x.Tasks).Returns(_mockTaskSet.Object);

        _handler = new GetTaskByIdQueryHandler(
            _mockContext.Object,
            _mockCurrentUserService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_EmptyGuid_ReturnsNull()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = Guid.Empty };
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GetTaskById called with empty GUID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = Guid.NewGuid() };
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal("User must be authenticated to get task.", exception.Message);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unauthorized access attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserIdIsNull_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = Guid.NewGuid() };
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal("User must be authenticated to get task.", exception.Message);
    }

    [Fact]
    public async Task Handle_TaskExists_ReturnsTaskDto()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetTaskByIdQuery { Id = taskId };

        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        var tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = taskId,
                UserId = userId,
                Title = "測試任務",
                Description = "測試描述",
                Status = TaskStatus.InProgress,
                Priority = Priority.High,
                EstimatedMinutes = 60,
                ActualMinutes = 30,
                DueDate = DateTime.UtcNow.AddDays(1),
                Tags = "tag1,tag2",
                Notes = "測試備註",
                IsRecurring = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                SubTasks = new List<TaskItem>
                {
                    new TaskItem { Status = TaskStatus.Completed },
                    new TaskItem { Status = TaskStatus.InProgress }
                }
            }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<TaskItem>>();
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Provider).Returns(tasks.Provider);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Expression).Returns(tasks.Expression);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.ElementType).Returns(tasks.ElementType);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.GetEnumerator()).Returns(tasks.GetEnumerator());

        _mockContext.Setup(x => x.Tasks).Returns(mockSet.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
        Assert.Equal("測試任務", result.Title);
        Assert.Equal("測試描述", result.Description);
        Assert.Equal(TaskStatus.InProgress, result.Status);
        Assert.Equal(Priority.High, result.Priority);
        Assert.Equal(60, result.EstimatedMinutes);
        Assert.Equal(30, result.ActualMinutes);
        Assert.Equal("tag1,tag2", result.Tags);
        Assert.Equal("測試備註", result.Notes);
        Assert.False(result.IsRecurring);
        Assert.Equal(2, result.SubTaskCount);
        Assert.Equal(1, result.CompletedSubTaskCount);

        // Verify debug log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully retrieved task")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ReturnsNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetTaskByIdQuery { Id = taskId };

        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        var tasks = new List<TaskItem>().AsQueryable();

        var mockSet = new Mock<DbSet<TaskItem>>();
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Provider).Returns(tasks.Provider);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Expression).Returns(tasks.Expression);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.ElementType).Returns(tasks.ElementType);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.GetEnumerator()).Returns(tasks.GetEnumerator());

        _mockContext.Setup(x => x.Tasks).Returns(mockSet.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);

        // Verify information log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Task") && v.ToString().Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TaskBelongsToOtherUser_ReturnsNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var query = new GetTaskByIdQuery { Id = taskId };

        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        var tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = taskId,
                UserId = otherUserId, // 不同的使用者
                Title = "其他人的任務",
                Description = "無權限存取",
                Status = TaskStatus.InProgress,
                Priority = Priority.Medium,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<TaskItem>>();
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Provider).Returns(tasks.Provider);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Expression).Returns(tasks.Expression);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.ElementType).Returns(tasks.ElementType);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.GetEnumerator()).Returns(tasks.GetEnumerator());

        _mockContext.Setup(x => x.Tasks).Returns(mockSet.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);

        // Verify information log was called indicating access denied
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found or user") && v.ToString().Contains("doesn't have access")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DatabaseException_LogsErrorAndRethrows()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = Guid.NewGuid() };
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var exception = new InvalidOperationException("Database connection failed");
        _mockTaskSet.As<IQueryable<TaskItem>>().Setup(m => m.Provider).Throws(exception);
        
        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Database connection failed", thrownException.Message);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving task")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_LogsDebugInformation()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetTaskByIdQuery { Id = taskId };

        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        var tasks = new List<TaskItem>().AsQueryable();
        var mockSet = new Mock<DbSet<TaskItem>>();
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Provider).Returns(tasks.Provider);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.Expression).Returns(tasks.Expression);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.ElementType).Returns(tasks.ElementType);
        mockSet.As<IQueryable<TaskItem>>().Setup(m => m.GetEnumerator()).Returns(tasks.GetEnumerator());

        _mockContext.Setup(x => x.Tasks).Returns(mockSet.Object);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify debug log for getting task was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Getting task") && v.ToString().Contains("for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}