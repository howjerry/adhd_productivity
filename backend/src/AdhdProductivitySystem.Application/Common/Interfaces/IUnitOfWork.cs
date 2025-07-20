using AdhdProductivitySystem.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace AdhdProductivitySystem.Application.Common.Interfaces;

/// <summary>
/// Unit of Work 介面，用於管理交易和協調多個 Repository 的操作
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Task Repository
    /// </summary>
    ITaskRepository Tasks { get; }

    /// <summary>
    /// CaptureItem Repository
    /// </summary>
    ICaptureItemRepository CaptureItems { get; }

    /// <summary>
    /// 通用 Repository 取得方法
    /// </summary>
    /// <typeparam name="TEntity">實體類型</typeparam>
    /// <returns>Repository 實例</returns>
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// 儲存所有變更
    /// </summary>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>受影響的記錄數</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 開始資料庫交易
    /// </summary>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>交易物件</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交交易
    /// </summary>
    /// <param name="cancellationToken">取消 Token</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滾交易
    /// </summary>
    /// <param name="cancellationToken">取消 Token</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 執行交易操作
    /// </summary>
    /// <param name="operation">要執行的操作</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>操作結果</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// 執行交易操作（無回傳值）
    /// </summary>
    /// <param name="operation">要執行的操作</param>
    /// <param name="cancellationToken">取消 Token</param>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查是否有未儲存的變更
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// 取得當前的交易物件（如果有的話）
    /// </summary>
    IDbContextTransaction? CurrentTransaction { get; }
}