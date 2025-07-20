using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// CaptureItem 特定的 Repository 實作
/// </summary>
public class CaptureItemRepository : Repository<CaptureItem>, ICaptureItemRepository
{
    public CaptureItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根據使用者 ID 取得捕獲項目
    /// </summary>
    public async Task<List<CaptureItemDto>> GetCaptureItemsAsync(
        Guid userId,
        CaptureType? type = null,
        Priority? priority = null,
        bool? isProcessed = null,
        string? tags = null,
        string? searchText = null,
        int page = 1,
        int pageSize = 50,
        string sortBy = "createdat",
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => c.UserId == userId);

        // 應用篩選條件
        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(c => c.Priority == priority.Value);
        }

        if (isProcessed.HasValue)
        {
            query = query.Where(c => c.IsProcessed == isProcessed.Value);
        }

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower()).ToList();
            
            query = query.Where(c => tagList.Any(tag => c.Tags.ToLower().Contains(tag)));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchTextLower = searchText.ToLower();
            query = query.Where(c => c.Content.ToLower().Contains(searchTextLower) || 
                                   (c.Context != null && c.Context.ToLower().Contains(searchTextLower)));
        }

        // 應用排序
        query = sortBy.ToLower() switch
        {
            "content" => sortDescending ? query.OrderByDescending(c => c.Content) : query.OrderBy(c => c.Content),
            "priority" => sortDescending ? query.OrderByDescending(c => c.Priority) : query.OrderBy(c => c.Priority),
            "type" => sortDescending ? query.OrderByDescending(c => c.Type) : query.OrderBy(c => c.Type),
            "updatedat" => sortDescending ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
            _ => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        // 應用分頁並投影到 DTO
        var captureItemDtos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new CaptureItemDto
            {
                Id = item.Id,
                Content = item.Content,
                Type = item.Type,
                Priority = item.Priority,
                Tags = item.Tags,
                Context = item.Context,
                EnergyLevel = item.EnergyLevel,
                Mood = item.Mood,
                IsUrgent = item.IsUrgent,
                IsProcessed = item.IsProcessed,
                ProcessedAt = item.ProcessedAt,
                UserId = item.UserId,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return captureItemDtos;
    }

    /// <summary>
    /// 根據使用者 ID 和捕獲項目 ID 取得捕獲項目
    /// </summary>
    public async Task<CaptureItemDto?> GetCaptureItemByIdAsync(Guid userId, Guid captureItemId, CancellationToken cancellationToken = default)
    {
        var captureItemDto = await _dbSet
            .Where(c => c.Id == captureItemId && c.UserId == userId)
            .Select(item => new CaptureItemDto
            {
                Id = item.Id,
                Content = item.Content,
                Type = item.Type,
                Priority = item.Priority,
                Tags = item.Tags,
                Context = item.Context,
                EnergyLevel = item.EnergyLevel,
                Mood = item.Mood,
                IsUrgent = item.IsUrgent,
                IsProcessed = item.IsProcessed,
                ProcessedAt = item.ProcessedAt,
                UserId = item.UserId,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return captureItemDto;
    }

    /// <summary>
    /// 取得使用者的未處理捕獲項目
    /// </summary>
    public async Task<List<CaptureItem>> GetUnprocessedItemsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && !c.IsProcessed)
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 取得使用者的緊急捕獲項目
    /// </summary>
    public async Task<List<CaptureItem>> GetUrgentItemsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && c.IsUrgent && !c.IsProcessed)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量更新捕獲項目的處理狀態
    /// </summary>
    public async Task<int> BulkUpdateProcessedStatusAsync(Guid userId, List<Guid> captureItemIds, bool isProcessed, CancellationToken cancellationToken = default)
    {
        var items = await _dbSet
            .Where(c => c.UserId == userId && captureItemIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var item in items)
        {
            item.IsProcessed = isProcessed;
            if (isProcessed && !item.ProcessedAt.HasValue)
            {
                item.ProcessedAt = DateTime.UtcNow;
            }
            else if (!isProcessed)
            {
                item.ProcessedAt = null;
            }
            item.UpdatedAt = DateTime.UtcNow;
            updatedCount++;
        }

        return updatedCount;
    }

    /// <summary>
    /// 檢查使用者是否有權限存取捕獲項目
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, Guid captureItemId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(c => c.Id == captureItemId && c.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// 取得使用者的捕獲項目統計
    /// </summary>
    public async Task<CaptureItemStatistics> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var allItems = await _dbSet
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        var todayItems = await _dbSet
            .Where(c => c.UserId == userId && 
                       c.CreatedAt >= today && 
                       c.CreatedAt < tomorrow)
            .CountAsync(cancellationToken);

        var urgentItems = await _dbSet
            .Where(c => c.UserId == userId && c.IsUrgent && !c.IsProcessed)
            .CountAsync(cancellationToken);

        return new CaptureItemStatistics
        {
            TotalItems = allItems.Count,
            ProcessedItems = allItems.Count(c => c.IsProcessed),
            UnprocessedItems = allItems.Count(c => !c.IsProcessed),
            UrgentItems = urgentItems,
            TodayItems = todayItems
        };
    }
}