namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// 快取服務介面，支援標籤式快取失效和 Cache Aside 模式
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 異步取得快取項目
    /// </summary>
    /// <typeparam name="T">快取項目類型</typeparam>
    /// <param name="key">快取鍵值</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>快取項目，如果不存在則返回 null</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 異步設定快取項目
    /// </summary>
    /// <typeparam name="T">快取項目類型</typeparam>
    /// <param name="key">快取鍵值</param>
    /// <param name="value">要快取的值</param>
    /// <param name="expiry">過期時間，若不指定則使用預設值</param>
    /// <param name="tags">快取標籤，用於批量失效</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 異步移除特定快取項目
    /// </summary>
    /// <param name="key">快取鍵值</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 異步移除符合模式的快取項目
    /// </summary>
    /// <param name="pattern">Redis 模式 (例如: "user:123:*")</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// 異步依標籤移除快取項目
    /// </summary>
    /// <param name="tag">快取標籤</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// 異步依多個標籤移除快取項目
    /// </summary>
    /// <param name="tags">快取標籤陣列</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得或設定快取項目 (Cache Aside 模式)
    /// </summary>
    /// <typeparam name="T">快取項目類型</typeparam>
    /// <param name="key">快取鍵值</param>
    /// <param name="factory">當快取不存在時的資料工廠方法</param>
    /// <param name="expiry">過期時間</param>
    /// <param name="tags">快取標籤</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>快取或新建立的資料</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 檢查快取項目是否存在
    /// </summary>
    /// <param name="key">快取鍵值</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>存在則為 true</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 設定快取項目的過期時間
    /// </summary>
    /// <param name="key">快取鍵值</param>
    /// <param name="expiry">過期時間</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}