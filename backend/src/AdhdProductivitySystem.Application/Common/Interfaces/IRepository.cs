using AdhdProductivitySystem.Domain.Common;
using System.Linq.Expressions;

namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// 通用 Repository 介面，定義基本的 CRUD 操作
/// </summary>
/// <typeparam name="TEntity">實體類型，必須繼承自 BaseEntity</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// 取得可查詢的實體集合
    /// </summary>
    IQueryable<TEntity> Query { get; }

    /// <summary>
    /// 根據 ID 取得實體
    /// </summary>
    /// <param name="id">實體 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>實體或 null</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據條件取得第一個實體
    /// </summary>
    /// <param name="predicate">查詢條件</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>實體或 null</returns>
    Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據條件取得實體列表
    /// </summary>
    /// <param name="predicate">查詢條件</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>實體列表</returns>
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分頁查詢
    /// </summary>
    /// <param name="predicate">查詢條件</param>
    /// <param name="orderBy">排序</param>
    /// <param name="page">頁數（從 1 開始）</param>
    /// <param name="pageSize">每頁數量</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>分頁結果</returns>
    Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync<TKey>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool orderDescending = false,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查實體是否存在
    /// </summary>
    /// <param name="predicate">查詢條件</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算符合條件的實體數量
    /// </summary>
    /// <param name="predicate">查詢條件</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>數量</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增實體
    /// </summary>
    /// <param name="entity">要新增的實體</param>
    void Add(TEntity entity);

    /// <summary>
    /// 異步新增實體
    /// </summary>
    /// <param name="entity">要新增的實體</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>新增的實體</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增多個實體
    /// </summary>
    /// <param name="entities">要新增的實體列表</param>
    void AddRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 更新實體
    /// </summary>
    /// <param name="entity">要更新的實體</param>
    void Update(TEntity entity);

    /// <summary>
    /// 更新多個實體
    /// </summary>
    /// <param name="entities">要更新的實體列表</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 刪除實體
    /// </summary>
    /// <param name="entity">要刪除的實體</param>
    void Remove(TEntity entity);

    /// <summary>
    /// 根據 ID 刪除實體
    /// </summary>
    /// <param name="id">實體 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除多個實體
    /// </summary>
    /// <param name="entities">要刪除的實體列表</param>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 軟刪除實體（如果實體支援軟刪除）
    /// </summary>
    /// <param name="id">實體 ID</param>
    /// <param name="cancellationToken">取消 Token</param>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}