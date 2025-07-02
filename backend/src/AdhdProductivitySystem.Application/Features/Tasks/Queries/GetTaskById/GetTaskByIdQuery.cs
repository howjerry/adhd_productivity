using AdhdProductivitySystem.Application.Common.DTOs;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTaskById;

/// <summary>
/// Query to get a specific task by ID
/// </summary>
public class GetTaskByIdQuery : IRequest<TaskDto?>
{
    /// <summary>
    /// Task ID to retrieve
    /// </summary>
    public Guid Id { get; set; }
}