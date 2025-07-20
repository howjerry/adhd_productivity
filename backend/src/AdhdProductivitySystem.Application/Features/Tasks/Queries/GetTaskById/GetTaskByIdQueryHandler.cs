using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTaskById;

/// <summary>
/// Handler for getting a single task by ID - optimized to prevent N+1 queries with caching support
/// </summary>
public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTaskByIdQueryHandler> _logger;
    private readonly ICacheService _cacheService;

    // 快取相關常數
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(15);
    private const string CacheKeyPrefix = "task";

    public GetTaskByIdQueryHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetTaskByIdQueryHandler> logger,
        ICacheService cacheService)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        // Validate input
        if (request.Id == Guid.Empty)
        {
            _logger.LogWarning("GetTaskById called with empty GUID");
            return null;
        }

        // Check authentication
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized access attempt to get task {TaskId}", request.Id);
            throw new UnauthorizedAccessException("User must be authenticated to get task.");
        }

        var userId = _currentUserService.UserId.Value;
        _logger.LogDebug("Getting task {TaskId} for user {UserId}", request.Id, userId);

        // 建立快取鍵值
        var cacheKey = BuildCacheKey(userId, request.Id);
        var cacheTags = new[] { $"user:{userId}", $"task:{request.Id}" };

        try
        {
            // 從資料庫查詢任務（不使用可能為 null 的快取）
            var taskDto = await _taskRepository.GetTaskByIdWithStatisticsAsync(
                userId,
                request.Id,
                cancellationToken);

            if (taskDto != null)
            {
                _logger.LogDebug("Task {TaskId} loaded from database for user {UserId}", request.Id, userId);
                
                // 快取非 null 結果
                await _cacheService.SetAsync(cacheKey, taskDto, CacheExpiry, cacheTags, cancellationToken);
            }

            if (taskDto == null)
            {
                _logger.LogInformation("Task {TaskId} not found or user {UserId} doesn't have access", request.Id, userId);
            }
            else
            {
                _logger.LogDebug("Successfully retrieved task {TaskId} for user {UserId} (from cache or database)", request.Id, userId);
            }

            return taskDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {TaskId} for user {UserId}", request.Id, userId);
            throw;
        }
    }

    /// <summary>
    /// 建立任務快取鍵值
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="taskId">任務 ID</param>
    /// <returns>快取鍵值</returns>
    private static string BuildCacheKey(Guid userId, Guid taskId)
    {
        return $"{CacheKeyPrefix}:{userId}:{taskId}";
    }
}