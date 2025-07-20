using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;
using Moq;

namespace AdhdProductivitySystem.Tests.Infrastructure.Services;

/// <summary>
/// Redis 快取系統驗證測試
/// 驗證 CacheService、CacheInvalidationService 和快取整合
/// </summary>
public class RedisCacheValidationTest
{
    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    [Fact]
    public async Task CacheService_BasicOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CacheService>>();
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new CacheService(distributedCache, null, mockLogger.Object);

        var testData = new TestData
        {
            Id = 1,
            Name = "Test Item",
            CreatedAt = DateTime.UtcNow
        };
        var cacheKey = "test:item:1";

        // Act & Assert - Set Cache
        await cacheService.SetAsync(cacheKey, testData, TimeSpan.FromMinutes(10));

        // Act & Assert - Get Cache Hit
        var cachedItem = await cacheService.GetAsync<TestData>(cacheKey);
        Assert.NotNull(cachedItem);
        Assert.Equal(testData.Id, cachedItem.Id);
        Assert.Equal(testData.Name, cachedItem.Name);

        // Act & Assert - Cache Exists
        var exists = await cacheService.ExistsAsync(cacheKey);
        Assert.True(exists);

        // Act & Assert - Remove Cache
        await cacheService.RemoveAsync(cacheKey);
        var removedItem = await cacheService.GetAsync<TestData>(cacheKey);
        Assert.Null(removedItem);
    }

    [Fact]
    public async Task CacheService_GetOrSetAsync_CacheMiss_ShouldCallFactory()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CacheService>>();
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new CacheService(distributedCache, null, mockLogger.Object);

        var testData = new TestData
        {
            Id = 2,
            Name = "Factory Item",
            CreatedAt = DateTime.UtcNow
        };
        var cacheKey = "test:factory:2";
        var factoryCalled = false;

        // Act
        var result = await cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                factoryCalled = true;
                await Task.Delay(10); // 模擬資料庫查詢
                return testData;
            },
            TimeSpan.FromMinutes(5)
        );

        // Assert
        Assert.True(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);

        // Verify item is cached
        var cachedItem = await cacheService.GetAsync<TestData>(cacheKey);
        Assert.NotNull(cachedItem);
        Assert.Equal(testData.Id, cachedItem.Id);
    }

    [Fact]
    public async Task CacheService_GetOrSetAsync_CacheHit_ShouldNotCallFactory()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CacheService>>();
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new CacheService(distributedCache, null, mockLogger.Object);

        var testData = new TestData
        {
            Id = 3,
            Name = "Cached Item",
            CreatedAt = DateTime.UtcNow
        };
        var cacheKey = "test:cached:3";

        // Pre-populate cache
        await cacheService.SetAsync(cacheKey, testData, TimeSpan.FromMinutes(5));

        var factoryCalled = false;

        // Act
        var result = await cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                factoryCalled = true;
                await Task.Delay(10);
                return new TestData { Id = 999, Name = "Should not be called" };
            },
            TimeSpan.FromMinutes(5)
        );

        // Assert
        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
    }

    [Fact]
    public async Task CacheInvalidationService_InvalidateOnTaskCreated_ShouldCallCacheService()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<CacheInvalidationService>>();
        var invalidationService = new CacheInvalidationService(
            mockCacheService.Object,
            mockLogger.Object);

        var userId = 123;

        // Act
        await invalidationService.InvalidateOnTaskCreatedAsync(userId);

        // Assert
        mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CacheInvalidationService_InvalidateOnTaskUpdated_ShouldCallCorrectMethods()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<CacheInvalidationService>>();
        var invalidationService = new CacheInvalidationService(
            mockCacheService.Object,
            mockLogger.Object);

        var userId = 123;
        var taskId = 456;

        // Act
        await invalidationService.InvalidateOnTaskUpdatedAsync(userId, taskId);

        // Assert
        // 應該呼叫使用者快取失效
        mockCacheService.Verify(
            c => c.InvalidateByTagAsync($"user:{userId}", It.IsAny<CancellationToken>()),
            Times.Once);

        mockCacheService.Verify(
            c => c.RemovePatternAsync($"tasks:user:{userId}:*", It.IsAny<CancellationToken>()),
            Times.Once);

        // 應該呼叫任務詳細快取失效
        mockCacheService.Verify(
            c => c.RemoveAsync($"task:detail:{taskId}", It.IsAny<CancellationToken>()),
            Times.Once);

        mockCacheService.Verify(
            c => c.RemovePatternAsync($"task:{taskId}:*", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CacheKeyGeneration_ShouldBeConsistent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        // Act
        var key1 = $"task:{userId}:{taskId}";
        var key2 = $"task:{userId}:{taskId}";

        // Assert
        Assert.Equal(key1, key2);
        Assert.Contains(userId.ToString(), key1);
        Assert.Contains(taskId.ToString(), key1);
    }

    [Fact]
    public async Task CacheService_ExceptionHandling_ShouldReturnNullAndNotThrow()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockLogger = new Mock<ILogger<CacheService>>();
        var cacheService = new CacheService(mockDistributedCache.Object, null, mockLogger.Object);

        // 設定 Mock 拋出例外
        mockDistributedCache
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache connection failed"));

        // Act & Assert
        var result = await cacheService.GetAsync<TestData>("test:key");
        Assert.Null(result);

        // Verify error was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MemoryCache_FallbackMode_ShouldWork()
    {
        // Arrange - 模擬開發環境沒有 Redis 的情況
        var mockLogger = new Mock<ILogger<CacheService>>();
        var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new CacheService(memoryCache, null, mockLogger.Object); // Redis 為 null

        var testData = new TestData
        {
            Id = 100,
            Name = "Memory Cache Test",
            CreatedAt = DateTime.UtcNow
        };
        var cacheKey = "test:memory:100";

        // Act
        await cacheService.SetAsync(cacheKey, testData, TimeSpan.FromMinutes(5));
        var cachedItem = await cacheService.GetAsync<TestData>(cacheKey);

        // Assert
        Assert.NotNull(cachedItem);
        Assert.Equal(testData.Id, cachedItem.Id);
        Assert.Equal(testData.Name, cachedItem.Name);
    }

    [Fact]
    public async Task CacheService_MultipleOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CacheService>>();
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheService = new CacheService(distributedCache, null, mockLogger.Object);

        var testItems = new List<TestData>();
        for (int i = 1; i <= 10; i++)
        {
            testItems.Add(new TestData
            {
                Id = i,
                Name = $"Item {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        // Act - 並行設定多個快取項目
        var tasks = testItems.Select(async item =>
        {
            var key = $"test:parallel:{item.Id}";
            await cacheService.SetAsync(key, item, TimeSpan.FromMinutes(10));
            return key;
        });

        var cacheKeys = await Task.WhenAll(tasks);

        // Assert - 驗證所有項目都被正確快取
        for (int i = 0; i < testItems.Count; i++)
        {
            var cachedItem = await cacheService.GetAsync<TestData>(cacheKeys[i]);
            Assert.NotNull(cachedItem);
            Assert.Equal(testItems[i].Id, cachedItem.Id);
            Assert.Equal(testItems[i].Name, cachedItem.Name);
        }

        // Clean up - 批量移除
        var removeTasks = cacheKeys.Select(key => cacheService.RemoveAsync(key));
        await Task.WhenAll(removeTasks);

        // Verify cleanup
        foreach (var key in cacheKeys)
        {
            var exists = await cacheService.ExistsAsync(key);
            Assert.False(exists);
        }
    }
}