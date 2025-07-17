using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Handler for creating a new task
/// </summary>
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateTaskCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to create tasks.");
        }

        // Validate parent task exists if specified
        if (request.ParentTaskId.HasValue)
        {
            var parentTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.ParentTaskId.Value && t.UserId == _currentUserService.UserId.Value, cancellationToken);

            if (parentTask == null)
            {
                throw new ArgumentException("Parent task not found or does not belong to the current user.");
            }
        }

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            EstimatedMinutes = request.EstimatedMinutes,
            DueDate = request.DueDate,
            Tags = request.Tags,
            Notes = request.Notes,
            ParentTaskId = request.ParentTaskId,
            IsRecurring = request.IsRecurring,
            RecurrencePattern = request.RecurrencePattern,
            UserId = _currentUserService.UserId.Value,
            CreatedBy = _currentUserService.UserEmail
        };

        // Set next occurrence for recurring tasks
        if (request.IsRecurring && !string.IsNullOrEmpty(request.RecurrencePattern))
        {
            task.NextOccurrence = CalculateNextOccurrence(DateTime.UtcNow, request.RecurrencePattern);
        }

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        // Load task with related data for mapping
        var createdTask = await _context.Tasks
            .Include(t => t.SubTasks)
            .FirstAsync(t => t.Id == task.Id, cancellationToken);

        var taskDto = _mapper.Map<TaskDto>(createdTask);
        taskDto.SubTaskCount = createdTask.SubTasks.Count;
        taskDto.CompletedSubTaskCount = createdTask.SubTasks.Count(st => st.Status == Domain.Enums.TaskStatus.Completed);

        return taskDto;
    }

    private DateTime? CalculateNextOccurrence(DateTime baseDate, string recurrencePattern)
    {
        // Simple implementation - can be enhanced with more complex patterns
        return recurrencePattern.ToLower() switch
        {
            "daily" => baseDate.AddDays(1),
            "weekly" => baseDate.AddDays(7),
            "monthly" => baseDate.AddMonths(1),
            "yearly" => baseDate.AddYears(1),
            _ => null
        };
    }
}