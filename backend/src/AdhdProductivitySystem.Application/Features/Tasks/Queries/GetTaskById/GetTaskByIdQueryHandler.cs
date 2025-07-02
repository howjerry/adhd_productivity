using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTaskById;

/// <summary>
/// Handler for getting a single task by ID - optimized to prevent N+1 queries
/// </summary>
public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTaskByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to get task.");
        }

        // Optimized query that projects directly to DTO to reduce memory usage
        // and uses a single query with calculated subtask counts
        var taskDto = await _context.Tasks
            .Where(t => t.Id == request.Id && t.UserId == _currentUserService.UserId.Value)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                EstimatedMinutes = t.EstimatedMinutes,
                ActualMinutes = t.ActualMinutes,
                DueDate = t.DueDate,
                CompletedAt = t.CompletedAt,
                Tags = t.Tags,
                Notes = t.Notes,
                IsRecurring = t.IsRecurring,
                RecurrencePattern = t.RecurrencePattern,
                NextOccurrence = t.NextOccurrence,
                UserId = t.UserId,
                ParentTaskId = t.ParentTaskId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                // Calculate subtask counts efficiently in the database
                SubTaskCount = t.SubTasks.Count(),
                CompletedSubTaskCount = t.SubTasks.Count(st => st.Status == Domain.Enums.TaskStatus.Completed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return taskDto;
    }
}