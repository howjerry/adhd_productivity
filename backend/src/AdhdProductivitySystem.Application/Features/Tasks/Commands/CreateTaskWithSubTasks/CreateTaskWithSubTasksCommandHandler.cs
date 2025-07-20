using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using TaskStatus = AdhdProductivitySystem.Domain.Enums.TaskStatus;

namespace AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTaskWithSubTasks;

/// <summary>
/// 創建任務並同時創建子任務的命令處理器
/// 示範 Unit of Work Pattern 的使用
/// </summary>
public class CreateTaskWithSubTasksCommandHandler : IRequestHandler<CreateTaskWithSubTasksCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateTaskWithSubTasksCommandHandler> _logger;

    public CreateTaskWithSubTasksCommandHandler(
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<CreateTaskWithSubTasksCommandHandler> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TaskDto> Handle(CreateTaskWithSubTasksCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to create tasks.");
        }

        var userId = _currentUserService.UserId.Value;

        // 使用 Unit of Work 執行交易操作
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _logger.LogInformation("Creating task with {SubTaskCount} subtasks for user {UserId}", 
                request.SubTasks.Count, userId);

            // 創建主任務
            var mainTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Status = TaskStatus.Todo,
                Priority = request.Priority,
                EstimatedMinutes = request.EstimatedMinutes,
                DueDate = request.DueDate,
                Tags = request.Tags ?? string.Empty,
                Notes = request.Notes,
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                UserId = userId,
                ParentTaskId = null
            };

            // 添加主任務到 repository
            _taskRepository.Add(mainTask);

            // 創建子任務
            var subTasks = new List<TaskItem>();
            foreach (var subTaskInfo in request.SubTasks)
            {
                var subTask = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = subTaskInfo.Title,
                    Description = subTaskInfo.Description,
                    Status = TaskStatus.Todo,
                    Priority = subTaskInfo.Priority,
                    EstimatedMinutes = subTaskInfo.EstimatedMinutes,
                    DueDate = subTaskInfo.DueDate,
                    Tags = string.Empty,
                    Notes = string.Empty,
                    IsRecurring = false,
                    RecurrencePattern = null,
                    UserId = userId,
                    ParentTaskId = mainTask.Id
                };

                subTasks.Add(subTask);
                _taskRepository.Add(subTask);
            }

            // 在此處可以添加其他業務邏輯，例如發送通知、更新統計等
            // 所有操作都會在同一個交易中執行

            _logger.LogInformation("Successfully created task {TaskId} with {SubTaskCount} subtasks", 
                mainTask.Id, subTasks.Count);

            // 使用 Repository 的優化方法取得任務，包含子任務統計
            var taskDto = await _taskRepository.GetTaskByIdWithStatisticsAsync(
                userId,
                mainTask.Id);

            if (taskDto == null)
            {
                throw new InvalidOperationException("Failed to retrieve created task with subtasks.");
            }

            return taskDto;

        }, cancellationToken);
    }
}