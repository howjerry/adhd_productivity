using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;

/// <summary>
/// Handler for getting tasks with integrated Redis caching
/// </summary>
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

        var userId = _currentUserService.UserId.Value;

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

        _logger.LogInformation("Retrieved {TaskCount} tasks for user {UserId}", tasks.Count, userId);
        return tasks;
    }

    /// <summary>
    /// 執行實際的資料庫查詢
    /// </summary>
    private async Task<List<TaskDto>> ExecuteQuery(GetTasksQuery request, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing database query for tasks, user: {UserId}", userId);

        // Build the base query with direct projection to DTO
        // This eliminates the N+1 pattern by calculating subtask counts in the database
        var query = _context.Tasks
            .Where(t => t.UserId == userId);

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        if (request.DueDateFrom.HasValue)
        {
            query = query.Where(t => t.DueDate >= request.DueDateFrom.Value);
        }

        if (request.DueDateTo.HasValue)
        {
            query = query.Where(t => t.DueDate <= request.DueDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            var tags = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower()).ToList();
            
            // Optimized tag search using ANY operator for better performance
            query = query.Where(t => tags.Any(tag => t.Tags.ToLower().Contains(tag)));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchText) || 
                                   (t.Description != null && t.Description.ToLower().Contains(searchText)));
        }

        if (!request.IncludeSubTasks)
        {
            query = query.Where(t => t.ParentTaskId == null);
        }

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "priority" => request.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "duedate" => request.SortDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "status" => request.SortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "updatedat" => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            _ => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };

        // CRITICAL PERFORMANCE FIX: Project directly to DTO with subtask counts calculated in database
        var taskDtos = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(task => new TaskDto
            {
                Id = task.Id,
                UserId = task.UserId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                EstimatedMinutes = task.EstimatedMinutes,
                ActualMinutes = task.ActualMinutes,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                Tags = task.Tags,
                Notes = task.Notes,
                IsRecurring = task.IsRecurring,
                RecurrencePattern = task.RecurrencePattern,
                NextOccurrence = task.NextOccurrence,
                ParentTaskId = task.ParentTaskId,
                SubTaskCount = task.SubTasks.Count(),
                CompletedSubTaskCount = task.SubTasks.Count(st => st.Status == Domain.Enums.TaskStatus.Completed),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return taskDtos;
    }

    /// <summary>
    /// 生成快取鍵值，包含查詢參數的雜湊
    /// </summary>
    private static string GenerateCacheKey(Guid userId, GetTasksQuery request)
    {
        // 建立查詢參數字串
        var queryString = $"{request.Status}_{request.Priority}_{request.DueDateFrom}_{request.DueDateTo}_" +
                         $"{request.Tags}_{request.SearchText}_{request.IncludeSubTasks}_{request.Page}_" +
                         $"{request.PageSize}_{request.SortBy}_{request.SortDescending}";
        
        // 生成雜湊以縮短鍵值長度
        var hash = GenerateHash(queryString);
        
        return $"tasks:user:{userId}:query:{hash}";
    }

    /// <summary>
    /// 使用 SHA256 生成字串雜湊
    /// </summary>
    private static string GenerateHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, 16); // 取前 16 個字元
    }
}