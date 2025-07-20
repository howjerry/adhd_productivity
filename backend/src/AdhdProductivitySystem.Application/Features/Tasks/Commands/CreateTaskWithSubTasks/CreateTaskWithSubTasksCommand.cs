using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTaskWithSubTasks;

/// <summary>
/// 創建任務並同時創建子任務的命令
/// 示範 Unit of Work 的使用
/// </summary>
public class CreateTaskWithSubTasksCommand : IRequest<TaskDto>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public int? EstimatedMinutes { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; } = false;
    public string? RecurrencePattern { get; set; }
    
    /// <summary>
    /// 子任務列表
    /// </summary>
    public List<SubTaskInfo> SubTasks { get; set; } = new();
}

/// <summary>
/// 子任務資訊
/// </summary>
public class SubTaskInfo
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public int? EstimatedMinutes { get; set; }
    public DateTime? DueDate { get; set; }
}