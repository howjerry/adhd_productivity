using AdhdProductivitySystem.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AdhdProductivitySystem.Tests.Infrastructure.Services;

public class CacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<CacheService>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
               .Returns(_mockDatabase.Object);

        _cacheService = new CacheService(
            _mockDistributedCache.Object,
            _mockRedis.Object,
            _mockLogger.Object);
    }

    public class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    [Fact]
    public async Task GetAsync_WhenCacheHit_ReturnsDeserializedObject()
    {
        // Arrange
        var key = "test:key";
        var testData = new TestDto { Id = 1, Name = "Test", CreatedAt = DateTime.UtcNow };
        var serializedData = JsonSerializer.Serialize(testData);

        _mockDistributedCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedData);

        // Act
        var result = await _cacheService.GetAsync<TestDto>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_WhenCacheMiss_ReturnsNull()
    {
        // Arrange
        var key = "test:key";

        _mockDistributedCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _cacheService.GetAsync<TestDto>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WithoutTags_CallsDistributedCacheSet()
    {
        // Arrange
        var key = "test:key";
        var testData = new TestDto { Id = 1, Name = "Test", CreatedAt = DateTime.UtcNow };
        var expectedSerialized = JsonSerializer.Serialize(testData);

        // Act
        await _cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(5));

        // Assert
        _mockDistributedCache.Verify(
            c => c.SetStringAsync(
                key,
                expectedSerialized,
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithTags_CallsDistributedCacheSetAndSetsTags()
    {
        // Arrange
        var key = "test:key";
        var testData = new TestDto { Id = 1, Name = "Test", CreatedAt = DateTime.UtcNow };
        var tags = new[] { "tag1", "tag2" };

        // Act
        await _cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(5), tags);

        // Assert
        _mockDistributedCache.Verify(
            c => c.SetStringAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify tag associations are set
        foreach (var tag in tags)
        {
            _mockDatabase.Verify(
                d => d.SetAddAsync($"tagged_keys:{tag}", key, It.IsAny<CommandFlags>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task RemoveAsync_CallsDistributedCacheRemove()
    {
        // Arrange
        var key = "test:key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDistributedCache.Verify(
            c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ReturnsFromCache()
    {
        // Arrange
        var key = "test:key";
        var cachedData = new TestDto { Id = 1, Name = "Cached", CreatedAt = DateTime.UtcNow };
        var serializedData = JsonSerializer.Serialize(cachedData);
        var factoryCalled = false;

        _mockDistributedCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedData);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            return new TestDto { Id = 2, Name = "Factory", CreatedAt = DateTime.UtcNow };
        });

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal(cachedData.Id, result.Id);
        Assert.Equal(cachedData.Name, result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_CallsFactoryAndCachesResult()
    {
        // Arrange
        var key = "test:key";
        var factoryData = new TestDto { Id = 2, Name = "Factory", CreatedAt = DateTime.UtcNow };
        var factoryCalled = false;

        _mockDistributedCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            return factoryData;
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.Equal(factoryData.Id, result.Id);
        Assert.Equal(factoryData.Name, result.Name);

        // Verify the result was cached
        _mockDistributedCache.Verify(
            c => c.SetStringAsync(
                key,
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateByTagAsync_RemovesAllTaggedKeys()
    {
        // Arrange
        var tag = "test-tag";
        var taggedKeys = new RedisValue[] { "key1", "key2", "key3" };

        _mockDatabase
            .Setup(d => d.SetMembersAsync($"tagged_keys:{tag}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(taggedKeys);

        // Act
        await _cacheService.InvalidateByTagAsync(tag);

        // Assert
        foreach (var key in taggedKeys)
        {
            _mockDistributedCache.Verify(
                c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        _mockDatabase.Verify(
            d => d.KeyDeleteAsync($"tagged_keys:{tag}", It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var key = "test:key";
        var data = Encoding.UTF8.GetBytes("some data");

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var key = "test:key";

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAsync_WhenExceptionOccurs_ReturnsNullAndLogsError()
    {
        // Arrange
        var key = "test:key";

        _mockDistributedCache
            .Setup(c => c.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        var result = await _cacheService.GetAsync<TestDto>(key);

        // Assert
        Assert.Null(result);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting cache for key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}