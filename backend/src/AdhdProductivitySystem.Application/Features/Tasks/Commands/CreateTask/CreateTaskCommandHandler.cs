using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AutoMapper;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Handler for creating a new task
/// </summary>
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
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
            var hasPermission = await _taskRepository.HasPermissionAsync(
                _currentUserService.UserId.Value, 
                request.ParentTaskId.Value, 
                cancellationToken);
            
            if (!hasPermission)
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

        _taskRepository.Add(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 使用 Repository 的優化方法取得任務，包含子任務統計
        var taskDto = await _taskRepository.GetTaskByIdWithStatisticsAsync(
            _currentUserService.UserId.Value,
            task.Id,
            cancellationToken);

        if (taskDto == null)
        {
            throw new InvalidOperationException("Failed to retrieve created task.");
        }

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