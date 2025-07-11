using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;

/// <summary>
/// Query to get tasks for the current user
/// </summary>
public class GetTasksQuery : IRequest<List<TaskDto>>
{
    /// <summary>
    /// Filter by task status
    /// </summary>
    public Domain.Enums.TaskStatus? Status { get; set; }

    /// <summary>
    /// Filter by priority
    /// </summary>
    public Priority? Priority { get; set; }

    /// <summary>
    /// Filter by due date range
    /// </summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>
    /// Filter by due date range
    /// </summary>
    public DateTime? DueDateTo { get; set; }

    /// <summary>
    /// Filter by tags (comma-separated)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Search text in title and description
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Include subtasks
    /// </summary>
    public bool IncludeSubTasks { get; set; } = true;

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort by field
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction
    /// </summary>
    public bool SortDescending { get; set; } = true;
}