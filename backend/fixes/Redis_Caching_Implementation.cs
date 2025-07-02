// REDIS CACHING IMPLEMENTATION
// Add these to implement Redis caching for performance improvement

// 1. Add NuGet package to AdhdProductivitySystem.Infrastructure.csproj:
// <PackageReference Include="StackExchange.Redis" Version="2.6.122" />
// <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="7.0.11" />

// 2. Add to Program.cs after line 38:
/*
// Configure Redis caching
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "ADHDProductivitySystem";
    });
}
else
{
    // Fallback to in-memory cache for development
    builder.Services.AddMemoryCache();
}

// Add caching service
builder.Services.AddScoped<ICacheService, CacheService>();
*/

// 3. Create ICacheService interface in AdhdProductivitySystem.Application/Common/Interfaces/:
using Microsoft.Extensions.Caching.Distributed;

namespace AdhdProductivitySystem.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);
}

// 4. Create CacheService implementation in AdhdProductivitySystem.Infrastructure/Services/:
using AdhdProductivitySystem.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AdhdProductivitySystem.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly DistributedCacheEntryOptions _defaultOptions;

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
        _defaultOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await _distributedCache.GetStringAsync(key, cancellationToken);
        return cached == null ? null : JsonSerializer.Deserialize<T>(cached);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = expiry.HasValue 
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }
            : _defaultOptions;

        await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: This is a simplified implementation
        // For Redis, consider using StackExchange.Redis directly for pattern-based operations
        // This would require injecting IConnectionMultiplexer and using Redis SCAN command
        throw new NotImplementedException("Pattern-based cache removal requires Redis-specific implementation");
    }
}

// 5. Update GetTasksQueryHandler to use caching:
/*
public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<TaskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetTasksQueryHandler> _logger;

    public GetTasksQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        ILogger<GetTasksQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to get tasks.");
        }

        // Create cache key based on user and query parameters
        var cacheKey = $"tasks:{_currentUserService.UserId}:{GetQueryHash(request)}";
        
        // Try to get from cache first
        var cachedTasks = await _cacheService.GetAsync<List<TaskDto>>(cacheKey, cancellationToken);
        if (cachedTasks != null)
        {
            _logger.LogInformation("Retrieved {TaskCount} tasks from cache for user {UserId}", 
                cachedTasks.Count, _currentUserService.UserId);
            return cachedTasks;
        }

        // If not in cache, execute query
        var tasks = await ExecuteQuery(request, cancellationToken);
        
        // Cache the results for 5 minutes
        await _cacheService.SetAsync(cacheKey, tasks, TimeSpan.FromMinutes(5), cancellationToken);
        
        _logger.LogInformation("Cached {TaskCount} tasks for user {UserId}", 
            tasks.Count, _currentUserService.UserId);
            
        return tasks;
    }

    private async Task<List<TaskDto>> ExecuteQuery(GetTasksQuery request, CancellationToken cancellationToken)
    {
        // ... your optimized query logic here ...
    }

    private string GetQueryHash(GetTasksQuery request)
    {
        // Create a hash of the query parameters for cache key
        var queryString = $"{request.Status}_{request.Priority}_{request.DueDateFrom}_{request.DueDateTo}_" +
                         $"{request.Tags}_{request.SearchText}_{request.IncludeSubTasks}_{request.Page}_" +
                         $"{request.PageSize}_{request.SortBy}_{request.SortDescending}";
        
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(queryString))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
*/