using AdhdProductivitySystem.Api.Models;
using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTaskById;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdhdProductivitySystem.Api.Controllers;

/// <summary>
/// Controller for managing tasks
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TasksController> _logger;

    public TasksController(IMediator mediator, ILogger<TasksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get tasks for the current user
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="priority">Filter by priority</param>
    /// <param name="dueDateFrom">Filter by due date from</param>
    /// <param name="dueDateTo">Filter by due date to</param>
    /// <param name="tags">Filter by tags (comma-separated)</param>
    /// <param name="searchText">Search text</param>
    /// <param name="includeSubTasks">Include subtasks</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort by field</param>
    /// <param name="sortDescending">Sort descending</param>
    /// <returns>List of tasks</returns>
    [HttpGet]
    [ResponseCache(Duration = 180, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Authorization", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<List<TaskDto>>> GetTasks(
        [FromQuery] Domain.Enums.TaskStatus? status = null,
        [FromQuery] Priority? priority = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null,
        [FromQuery] string? tags = null,
        [FromQuery] string? searchText = null,
        [FromQuery] bool includeSubTasks = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true)
    {
        try
        {
            var query = new GetTasksQuery
            {
                Status = status,
                Priority = priority,
                DueDateFrom = dueDateFrom,
                DueDateTo = dueDateTo,
                Tags = tags,
                SearchText = searchText,
                IncludeSubTasks = includeSubTasks,
                Page = page,
                PageSize = Math.Min(pageSize, 100), // Limit page size
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var tasks = await _mediator.Send(query);
            return Ok(ApiResponse<List<TaskDto>>.CreateSuccess(tasks));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse.CreateError(
                "Unauthorized",
                "需要有效的身分驗證才能存取任務清單"
            ));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "獲取任務清單失敗 - ErrorId: {ErrorId}", errorId);
            return StatusCode(500, ApiResponse.CreateError(
                "GetTasksError",
                "獲取任務清單時發生錯誤，請稍後再試",
                errorId
            ));
        }
    }

    /// <summary>
    /// 根據 ID 取得特定任務的詳細資訊
    /// </summary>
    /// <param name="id">任務 ID（必須是有效的 GUID 格式）</param>
    /// <returns>任務詳細資訊，包含子任務統計數據</returns>
    /// <remarks>
    /// 此端點會回傳指定任務的完整資訊，包括：
    /// - 基本任務資訊（標題、描述、狀態、優先級等）
    /// - 時間相關資訊（估計時間、實際時間、到期日等）
    /// - 子任務統計（總數及完成數量）
    /// - 重複任務設定（如果適用）
    /// 
    /// 注意事項：
    /// - 只能查詢屬於當前使用者的任務
    /// - 使用快取機制，可能會有最多 15 分鐘的資料延遲
    /// - 查詢已優化，避免 N+1 查詢問題
    /// 
    /// 範例請求：
    /// GET /api/tasks/550e8400-e29b-41d4-a716-446655440000
    /// 
    /// 範例回應：
    /// {
    ///   "id": "550e8400-e29b-41d4-a716-446655440000",
    ///   "title": "完成專案文件",
    ///   "description": "撰寫使用者手冊和技術文件",
    ///   "status": "InProgress",
    ///   "priority": "High",
    ///   "estimatedMinutes": 240,
    ///   "actualMinutes": 120,
    ///   "dueDate": "2024-01-15T10:00:00Z",
    ///   "tags": "工作,文件,重要",
    ///   "subTaskCount": 3,
    ///   "completedSubTaskCount": 1,
    ///   "isRecurring": false,
    ///   "createdAt": "2024-01-10T09:00:00Z",
    ///   "updatedAt": "2024-01-12T14:30:00Z"
    /// }
    /// </remarks>
    /// <response code="200">成功回傳任務資訊</response>
    /// <response code="400">請求格式錯誤或無效的 GUID</response>
    /// <response code="401">未授權存取，需要有效的 JWT Token</response>
    /// <response code="404">任務不存在或無權限存取</response>
    /// <response code="429">請求次數過多，已觸發速率限制</response>
    /// <response code="500">伺服器內部錯誤</response>
    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 300, VaryByHeader = "Authorization", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(Guid id)
    {
        try
        {
            // 驗證 GUID 格式（雖然路由約束已經處理，但加強驗證）
            if (id == Guid.Empty)
            {
                _logger.LogWarning("GetTask called with empty GUID");
                return BadRequest(ApiResponse.CreateError(
                    "InvalidTaskId",
                    "任務 ID 不能為空值"
                ));
            }

            var query = new GetTaskByIdQuery { Id = id };
            var task = await _mediator.Send(query);

            if (task == null)
            {
                _logger.LogInformation("Task {TaskId} not found or access denied", id);
                return NotFound(ApiResponse.CreateError(
                    "TaskNotFound",
                    $"找不到 ID 為 {id} 的任務，或您沒有存取權限",
                    details: new { taskId = id }
                ));
            }

            _logger.LogDebug("Successfully returned task {TaskId}", id);
            return Ok(ApiResponse<TaskDto>.CreateSuccess(task));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to task {TaskId}", id);
            return Unauthorized(ApiResponse.CreateError(
                "Unauthorized",
                "需要有效的身分驗證才能存取任務詳細資訊"
            ));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for task {TaskId}", id);
            return BadRequest(ApiResponse.CreateError(
                "InvalidRequest",
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "取得任務失敗 - ErrorId: {ErrorId}, TaskId: {TaskId}", errorId, id);
            return StatusCode(500, ApiResponse.CreateError(
                "InternalServerError",
                "取得任務時發生內部錯誤，請稍後再試",
                errorId,
                new { taskId = id }
            ));
        }
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    /// <param name="command">Task creation data</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask([FromBody] CreateTaskCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                return BadRequest(ApiResponse.CreateError(
                    "ValidationError",
                    "請求資料格式不正確",
                    details: new { ValidationErrors = validationErrors }
                ));
            }

            var task = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, ApiResponse<TaskDto>.CreateSuccess(task));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse.CreateError(
                "Unauthorized",
                "需要有效的身分驗證才能建立任務"
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.CreateError(
                "InvalidArgument",
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "建立任務失敗 - ErrorId: {ErrorId}", errorId);
            return StatusCode(500, ApiResponse.CreateError(
                "CreateTaskError",
                "建立任務時發生錯誤，請稍後再試",
                errorId
            ));
        }
    }

    /// <summary>
    /// Update a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="command">Task update data</param>
    /// <returns>Updated task</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(Guid id, [FromBody] UpdateTaskCommand command)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest(ApiResponse.CreateError(
                    "TaskIdMismatch",
                    "路徑中的任務 ID 與請求內容不匹配"
                ));
            }

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                return BadRequest(ApiResponse.CreateError(
                    "ValidationError",
                    "請求資料格式不正確",
                    details: new { ValidationErrors = validationErrors }
                ));
            }

            var task = await _mediator.Send(command);
            return Ok(ApiResponse<TaskDto>.CreateSuccess(task));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse.CreateError(
                "Unauthorized",
                "需要有效的身分驗證才能更新任務"
            ));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.CreateError(
                "TaskNotFound",
                $"找不到 ID 為 {id} 的任務",
                details: new { taskId = id }
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.CreateError(
                "InvalidArgument",
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "更新任務失敗 - ErrorId: {ErrorId}, TaskId: {TaskId}", errorId, id);
            return StatusCode(500, ApiResponse.CreateError(
                "UpdateTaskError",
                "更新任務時發生錯誤，請稍後再試",
                errorId,
                new { taskId = id }
            ));
        }
    }

    /// <summary>
    /// Delete a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        try
        {
            var command = new DeleteTaskCommand { Id = id };
            await _mediator.Send(command);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse.CreateError(
                "Unauthorized",
                "需要有效的身分驗證才能刪除任務"
            ));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.CreateError(
                "TaskNotFound",
                $"找不到 ID 為 {id} 的任務",
                details: new { taskId = id }
            ));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "刪除任務失敗 - ErrorId: {ErrorId}, TaskId: {TaskId}", errorId, id);
            return StatusCode(500, ApiResponse.CreateError(
                "DeleteTaskError",
                "刪除任務時發生錯誤，請稍後再試",
                errorId,
                new { taskId = id }
            ));
        }
    }

    /// <summary>
    /// Update task status
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated task</returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTaskStatus(Guid id, [FromBody] Domain.Enums.TaskStatus status)
    {
        try
        {
            var command = new UpdateTaskStatusCommand { Id = id, Status = status };
            var task = await _mediator.Send(command);
            return Ok(ApiResponse<TaskDto>.CreateSuccess(task));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse.CreateError(
                "Unauthorized",
                "需要有效的身分驗證才能更新任務狀態"
            ));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.CreateError(
                "TaskNotFound",
                $"找不到 ID 為 {id} 的任務",
                details: new { taskId = id }
            ));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "更新任務狀態失敗 - ErrorId: {ErrorId}, TaskId: {TaskId}", errorId, id);
            return StatusCode(500, ApiResponse.CreateError(
                "UpdateTaskStatusError",
                "更新任務狀態時發生錯誤，請稍後再試",
                errorId,
                new { taskId = id }
            ));
        }
    }
}

// Placeholder command classes that would be implemented
public class UpdateTaskCommand : IRequest<TaskDto>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public int? EstimatedMinutes { get; set; }
    public DateTime? DueDate { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class DeleteTaskCommand : IRequest
{
    public Guid Id { get; set; }
}

public class UpdateTaskStatusCommand : IRequest<TaskDto>
{
    public Guid Id { get; set; }
    public Domain.Enums.TaskStatus Status { get; set; }
}