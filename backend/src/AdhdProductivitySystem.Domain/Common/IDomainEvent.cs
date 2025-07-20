namespace AdhdProductivitySystem.Domain.Common;

/// <summary>
/// 領域事件基礎介面
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// 事件發生時間
    /// </summary>
    DateTime OccurredAt { get; }
}