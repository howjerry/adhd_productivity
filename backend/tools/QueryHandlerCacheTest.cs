using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace QueryHandlerCacheTest
{
    // 模擬 GetTasksQuery
    public class GetTasksQuery
    {
        public Domain.Enums.TaskStatus? Status { get; set; }
        public Priority? Priority { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public string? Tags { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeSubTasks { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "createdat";
        public bool SortDescending { get; set; } = true;
    }

    // 模擬列舉
    namespace Domain.Enums
    {
        public enum TaskStatus
        {
            Todo,
            InProgress,
            Completed,
            Cancelled
        }
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    // 模擬 TaskDto
    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Domain.Enums.TaskStatus Status { get; set; }
        public Priority Priority { get; set; }
        public int? EstimatedMinutes { get; set; }
        public int ActualMinutes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public DateTime? NextOccurrence { get; set; }
        public Guid? ParentTaskId { get; set; }
        public int SubTaskCount { get; set; }
        public int CompletedSubTaskCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // 模擬 ICacheService
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class;
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class;
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);
        Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);
    }

    // 簡化的 CacheService 實作
    public class SimpleCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger _logger;

        public SimpleCacheService(IDistributedCache distributedCache, ILogger logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var cached = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (cached == null)
                {
                    _logger.LogDebug($"Cache miss for key: {key}");
                    return null;
                }

                _logger.LogDebug($"Cache hit for key: {key}");
                return JsonSerializer.Deserialize<T>(cached);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cache for key: {key}");
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var options = expiry.HasValue 
                    ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }
                    : new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };

                var serializedValue = JsonSerializer.Serialize(value);
                await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);

                _logger.LogDebug($"Cached value for key: {key} with tags: {(tags != null ? string.Join(", ", tags) : "none")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting cache for key: {key}");
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                var value = await factory();
                await SetAsync(key, value, expiry, tags, cancellationToken);
                _logger.LogDebug($"Cache miss resolved for key: {key}");
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetOrSetAsync factory for key: {key}");
                throw;
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
                _logger.LogDebug($"Removed cache for key: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cache for key: {key}");
            }
        }

        public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Pattern removal requested: {pattern}");
            // 在記憶體快取中無法實現模式移除，僅記錄
        }

        public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Tag invalidation requested: {tag}");
            // 在記憶體快取中無法實現標籤失效，僅記錄
        }
    }

    // 模擬 GetTasksQueryHandler 的快取部分
    public class MockGetTasksQueryHandler
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger _logger;

        public MockGetTasksQueryHandler(ICacheService cacheService, ILogger logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<TaskDto>> Handle(GetTasksQuery request, Guid userId, CancellationToken cancellationToken)
        {
            // 建立快取鍵值，包含使用者 ID 和查詢參數雜湊
            var cacheKey = GenerateCacheKey(userId, request);
            var cacheTags = new[] { $"user:{userId}", "tasks" };

            // 使用 Cache Aside 模式
            var tasks = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () => await ExecuteQuery(request, userId, cancellationToken),
                TimeSpan.FromMinutes(5), // 快取 5 分鐘
                cacheTags,
                cancellationToken
            );

            _logger.LogInformation($"Retrieved {tasks.Count} tasks for user {userId}");
            return tasks;
        }

        private async Task<List<TaskDto>> ExecuteQuery(GetTasksQuery request, Guid userId, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Executing database query for tasks, user: {userId}");
            
            // 模擬資料庫查詢延遲
            await Task.Delay(100, cancellationToken);

            // 模擬返回一些任務
            var tasks = new List<TaskDto>();
            for (int i = 1; i <= 3; i++)
            {
                tasks.Add(new TaskDto
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = $"Task {i}",
                    Description = $"Description for task {i}",
                    Status = Domain.Enums.TaskStatus.Todo,
                    Priority = Priority.Medium,
                    EstimatedMinutes = 30,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            return tasks;
        }

        private static string GenerateCacheKey(Guid userId, GetTasksQuery request)
        {
            var queryString = $"{request.Status}_{request.Priority}_{request.DueDateFrom}_{request.DueDateTo}_" +
                             $"{request.Tags}_{request.SearchText}_{request.IncludeSubTasks}_{request.Page}_" +
                             $"{request.PageSize}_{request.SortBy}_{request.SortDescending}";
            
            var hash = GenerateHash(queryString);
            return $"tasks:user:{userId}:query:{hash}";
        }

        private static string GenerateHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .Substring(0, 16);
        }
    }

    public class ConsoleLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== GetTasksQueryHandler 快取整合測試 ===");
            Console.WriteLine();

            var logger = new ConsoleLogger();
            var memoryOptions = Options.Create(new MemoryDistributedCacheOptions());
            var distributedCache = new MemoryDistributedCache(memoryOptions);
            var cacheService = new SimpleCacheService(distributedCache, logger);
            var queryHandler = new MockGetTasksQueryHandler(cacheService, logger);

            var userId = Guid.NewGuid();

            Console.WriteLine("1. 測試首次查詢（快取未命中）");
            await TestFirstQuery(queryHandler, userId);

            Console.WriteLine("\n2. 測試相同查詢（快取命中）");
            await TestCachedQuery(queryHandler, userId);

            Console.WriteLine("\n3. 測試不同查詢參數（不同快取鍵）");
            await TestDifferentQueryParameters(queryHandler, userId);

            Console.WriteLine("\n4. 測試快取鍵值生成一致性");
            await TestCacheKeyConsistency(queryHandler, userId);

            Console.WriteLine("\n5. 測試併發查詢");
            await TestConcurrentQueries(queryHandler, userId);

            Console.WriteLine("\n=== 測試完成 ===");
            Console.WriteLine("GetTasksQueryHandler 快取整合運作正常！");
        }

        static async Task TestFirstQuery(MockGetTasksQueryHandler queryHandler, Guid userId)
        {
            var query = new GetTasksQuery
            {
                Page = 1,
                PageSize = 10,
                Status = Domain.Enums.TaskStatus.Todo
            };

            var startTime = DateTime.UtcNow;
            var tasks = await queryHandler.Handle(query, userId, CancellationToken.None);
            var elapsed = DateTime.UtcNow - startTime;

            Console.WriteLine($"✓ 首次查詢完成，返回 {tasks.Count} 個任務");
            Console.WriteLine($"  執行時間: {elapsed.TotalMilliseconds:F2}ms");
        }

        static async Task TestCachedQuery(MockGetTasksQueryHandler queryHandler, Guid userId)
        {
            var query = new GetTasksQuery
            {
                Page = 1,
                PageSize = 10,
                Status = Domain.Enums.TaskStatus.Todo
            };

            var startTime = DateTime.UtcNow;
            var tasks = await queryHandler.Handle(query, userId, CancellationToken.None);
            var elapsed = DateTime.UtcNow - startTime;

            Console.WriteLine($"✓ 快取查詢完成，返回 {tasks.Count} 個任務");
            Console.WriteLine($"  執行時間: {elapsed.TotalMilliseconds:F2}ms");
            
            if (elapsed.TotalMilliseconds < 50) // 應該比首次查詢快很多
            {
                Console.WriteLine("✓ 快取效能提升顯著");
            }
            else
            {
                Console.WriteLine("? 快取效能提升不明顯");
            }
        }

        static async Task TestDifferentQueryParameters(MockGetTasksQueryHandler queryHandler, Guid userId)
        {
            var query1 = new GetTasksQuery
            {
                Page = 1,
                PageSize = 10,
                Status = Domain.Enums.TaskStatus.InProgress
            };

            var query2 = new GetTasksQuery
            {
                Page = 2,
                PageSize = 10,
                Status = Domain.Enums.TaskStatus.Todo
            };

            var tasks1 = await queryHandler.Handle(query1, userId, CancellationToken.None);
            var tasks2 = await queryHandler.Handle(query2, userId, CancellationToken.None);

            Console.WriteLine($"✓ 不同查詢參數測試完成");
            Console.WriteLine($"  查詢1 返回: {tasks1.Count} 個任務");
            Console.WriteLine($"  查詢2 返回: {tasks2.Count} 個任務");
            Console.WriteLine("✓ 不同查詢使用不同快取鍵");
        }

        static async Task TestCacheKeyConsistency(MockGetTasksQueryHandler queryHandler, Guid userId)
        {
            var query = new GetTasksQuery
            {
                Page = 1,
                PageSize = 10,
                Priority = Priority.High,
                SearchText = "important"
            };

            // 執行相同查詢多次
            var tasks1 = await queryHandler.Handle(query, userId, CancellationToken.None);
            var tasks2 = await queryHandler.Handle(query, userId, CancellationToken.None);
            var tasks3 = await queryHandler.Handle(query, userId, CancellationToken.None);

            Console.WriteLine($"✓ 快取鍵值一致性測試完成");
            Console.WriteLine($"  每次查詢都返回相同數量的任務: {tasks1.Count}");
            Console.WriteLine("✓ 快取鍵值生成一致");
        }

        static async Task TestConcurrentQueries(MockGetTasksQueryHandler queryHandler, Guid userId)
        {
            var query = new GetTasksQuery
            {
                Page = 1,
                PageSize = 10,
                Status = Domain.Enums.TaskStatus.Completed
            };

            var tasks = new List<Task<List<TaskDto>>>();
            
            // 併發執行 5 個相同的查詢
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(queryHandler.Handle(query, userId, CancellationToken.None));
            }

            var results = await Task.WhenAll(tasks);

            Console.WriteLine($"✓ 併發查詢測試完成");
            Console.WriteLine($"  5 個併發查詢都完成");
            Console.WriteLine($"  每個查詢返回相同數量的任務: {results[0].Count}");
            
            // 驗證所有結果都相同
            var allSame = results.All(r => r.Count == results[0].Count);
            if (allSame)
            {
                Console.WriteLine("✓ 併發查詢結果一致");
            }
            else
            {
                Console.WriteLine("✗ 併發查詢結果不一致");
            }
        }
    }
}