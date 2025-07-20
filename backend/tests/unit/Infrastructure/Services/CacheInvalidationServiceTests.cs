using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdhdProductivitySystem.Tests.Infrastructure.Services;

public class CacheInvalidationServiceTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CacheInvalidationService>> _mockLogger;
    private readonly CacheInvalidationService _cacheInvalidationService;

    public CacheInvalidationServiceTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CacheInvalidationService>>();
        _cacheInvalidationService = new CacheInvalidationService(
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task InvalidateOnTaskCreatedAsync_CallsInvalidateUserTaskCache()
    {
        // Arrange
        var userId = 123;

        // Act
        await _cacheInvalidationService.InvalidateOnTaskCreatedAsync(userId);

        // Assert
        _mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateOnTaskUpdatedAsync_CallsCorrectInvalidationMethods()
    {
        // Arrange
        var userId = 123;
        var taskId = 456;

        // Act
        await _cacheInvalidationService.InvalidateOnTaskUpdatedAsync(userId, taskId);

        // Assert
        _mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemoveAsync($"task:detail:{taskId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"task:{taskId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateOnTaskDeletedAsync_CallsCorrectInvalidationMethods()
    {
        // Arrange
        var userId = 123;
        var taskId = 456;

        // Act
        await _cacheInvalidationService.InvalidateOnTaskDeletedAsync(userId, taskId);

        // Assert
        _mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemoveAsync($"task:detail:{taskId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"task:{taskId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateOnSubTaskChangedAsync_WithParentTaskId_CallsCorrectInvalidationMethods()
    {
        // Arrange
        var userId = 123;
        var parentTaskId = 456;

        // Act
        await _cacheInvalidationService.InvalidateOnSubTaskChangedAsync(userId, parentTaskId);

        // Assert
        _mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemoveAsync($"task:detail:{parentTaskId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"task:{parentTaskId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateOnSubTaskChangedAsync_WithoutParentTaskId_OnlyInvalidatesUserCache()
    {
        // Arrange
        var userId = 123;

        // Act
        await _cacheInvalidationService.InvalidateOnSubTaskChangedAsync(userId, null);

        // Assert
        _mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);

        // Should not call task detail invalidation methods
        _mockCacheService.Verify(
            c => c.RemoveAsync(It.Is<string>(s => s.StartsWith("task:detail:")), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvalidateUserTaskCacheAsync_CallsCorrectMethods()
    {
        // Arrange
        var userId = 123;

        // Act
        await _cacheInvalidationService.InvalidateUserTaskCacheAsync(userId);

        // Assert
        _mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateTaskDetailCacheAsync_CallsCorrectMethods()
    {
        // Arrange
        var taskId = 456;

        // Act
        await _cacheInvalidationService.InvalidateTaskDetailCacheAsync(taskId);

        // Assert
        _mockCacheService.Verify(
            c => c.RemoveAsync($"task:detail:{taskId}", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCacheService.Verify(
            c => c.RemovePatternAsync($"task:{taskId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateOnTaskCreatedAsync_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var userId = 123;
        _mockCacheService
            .Setup(c => c.InvalidateByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        await _cacheInvalidationService.InvalidateOnTaskCreatedAsync(userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error invalidating cache on task created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}