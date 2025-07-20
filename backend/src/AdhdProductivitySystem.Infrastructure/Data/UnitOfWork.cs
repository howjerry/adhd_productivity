using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// Unit of Work 實作，用於管理交易和協調多個 Repository 的操作
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Lazy initialization for repositories
    private readonly Lazy<ITaskRepository> _tasks;
    private readonly Lazy<ICaptureItemRepository> _captureItems;

    public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositories = new Dictionary<Type, object>();
        
        // Initialize lazy repositories
        _tasks = new Lazy<ITaskRepository>(() => new TaskRepository(_context));
        _captureItems = new Lazy<ICaptureItemRepository>(() => new CaptureItemRepository(_context));
    }

    /// <summary>
    /// Task Repository
    /// </summary>
    public ITaskRepository Tasks => _tasks.Value;

    /// <summary>
    /// CaptureItem Repository
    /// </summary>
    public ICaptureItemRepository CaptureItems => _captureItems.Value;

    /// <summary>
    /// 通用 Repository 取得方法
    /// </summary>
    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<TEntity>(_context);
        }
        
        return (IRepository<TEntity>)_repositories[type];
    }

    /// <summary>
    /// 儲存所有變更
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} entities to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving changes to database");
            throw;
        }
    }

    /// <summary>
    /// 開始資料庫交易
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Database transaction started");
        return _transaction;
    }

    /// <summary>
    /// 提交交易
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while committing transaction");
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// 回滾交易
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Database transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while rolling back transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// 執行交易操作
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // 如果已經有交易在進行中，直接執行操作
        if (_transaction != null)
        {
            return await operation();
        }

        // 開始新的交易
        await BeginTransactionAsync(cancellationToken);
        
        try
        {
            var result = await operation();
            await SaveChangesAsync(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during transaction operation");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 執行交易操作（無回傳值）
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // 如果已經有交易在進行中，直接執行操作
        if (_transaction != null)
        {
            await operation();
            return;
        }

        // 開始新的交易
        await BeginTransactionAsync(cancellationToken);
        
        try
        {
            await operation();
            await SaveChangesAsync(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during transaction operation");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 檢查是否有未儲存的變更
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            return _context.ChangeTracker.HasChanges();
        }
    }

    /// <summary>
    /// 取得當前的交易物件（如果有的話）
    /// </summary>
    public IDbContextTransaction? CurrentTransaction => _transaction;

    /// <summary>
    /// 釋放資源
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose transaction if it exists
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            // Clear repositories
            _repositories.Clear();

            _disposed = true;
        }
    }

    /// <summary>
    /// 釋放資源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 非同步釋放資源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        _repositories.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}