using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// CaptureItem 特定的 Repository 介面
/// </summary>
public interface ICaptureItemRepository : IRepository<CaptureItem>
{
    /// <summary>
    /// 根據使用者 ID 取得捕獲項目
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="type">類型篩選</param>
    /// <param name="priority">優先級篩選</param>
    /// <param name="isProcessed">是否已處理篩選</param>
    /// <param name="tags">標籤篩選（逗號分隔）</param>
    /// <param name="searchText">搜尋文字</param>
    /// <param name="page">頁數</param>
    /// <param name="pageSize">每頁數量</param>
    /// <param name="sortBy">排序欄位</param>
    /// <param name="sortDescending">是否降序</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>捕獲項目 DTO 列表</returns>
    Task<List<CaptureItemDto>> GetCaptureItemsAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據使用者 ID 和捕獲項目 ID 取得捕獲項目
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="captureItemId">捕獲項目 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>捕獲項目 DTO 或 null</returns>
    Task<CaptureItemDto?> GetCaptureItemByIdAsync(Guid userId, Guid captureItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的未處理捕獲項目
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>未處理捕獲項目列表</returns>
    Task<List<CaptureItem>> GetUnprocessedItemsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的緊急捕獲項目
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>緊急捕獲項目列表</returns>
    Task<List<CaptureItem>> GetUrgentItemsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新捕獲項目的處理狀態
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="captureItemIds">捕獲項目 ID 列表</param>
    /// <param name="isProcessed">處理狀態</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>更新的項目數量</returns>
    Task<int> BulkUpdateProcessedStatusAsync(Guid userId, List<Guid> captureItemIds, bool isProcessed, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查使用者是否有權限存取捕獲項目
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="captureItemId">捕獲項目 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>是否有權限</returns>
    Task<bool> HasPermissionAsync(Guid userId, Guid captureItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的捕獲項目統計
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>捕獲項目統計</returns>
    Task<CaptureItemStatistics> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 捕獲項目統計
/// </summary>
public class CaptureItemStatistics
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int UnprocessedItems { get; set; }
    public int UrgentItems { get; set; }
    public int TodayItems { get; set; }
    public double ProcessingRate => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
}