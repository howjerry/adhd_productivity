using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdhdProductivitySystem.Api.Controllers;

/// <summary>
/// Controller for managing tasks
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TaskDto>>> GetTasks(
        [FromQuery] TaskStatus? status = null,
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
            return Ok(tasks);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks");
            return StatusCode(500, "An error occurred while getting tasks");
        }
    }

    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Task details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskDto>> GetTask(Guid id)
    {
        try
        {
            // This would be implemented as a separate query
            var query = new GetTasksQuery { SearchText = id.ToString() };
            var tasks = await _mediator.Send(query);
            var task = tasks.FirstOrDefault(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId}", id);
            return StatusCode(500, "An error occurred while getting the task");
        }
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    /// <param name="command">Task creation data</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, "An error occurred while creating the task");
        }
    }

    /// <summary>
    /// Update a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="command">Task update data</param>
    /// <returns>Updated task</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskCommand command)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest("Task ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task = await _mediator.Send(command);
            return Ok(task);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", id);
            return StatusCode(500, "An error occurred while updating the task");
        }
    }

    /// <summary>
    /// Delete a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            return Unauthorized();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            return StatusCode(500, "An error occurred while deleting the task");
        }
    }

    /// <summary>
    /// Update task status
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated task</returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskDto>> UpdateTaskStatus(Guid id, [FromBody] TaskStatus status)
    {
        try
        {
            var command = new UpdateTaskStatusCommand { Id = id, Status = status };
            var task = await _mediator.Send(command);
            return Ok(task);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task status {TaskId}", id);
            return StatusCode(500, "An error occurred while updating the task status");
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
    public TaskStatus Status { get; set; }
}