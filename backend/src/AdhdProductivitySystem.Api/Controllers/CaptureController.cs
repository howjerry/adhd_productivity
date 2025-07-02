using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Features.CaptureItems.Commands.CreateCaptureItem;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdhdProductivitySystem.Api.Controllers;

/// <summary>
/// Controller for managing capture items (brain dump)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaptureController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CaptureController> _logger;

    public CaptureController(IMediator mediator, ILogger<CaptureController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get capture items for the current user
    /// </summary>
    /// <param name="type">Filter by type</param>
    /// <param name="isProcessed">Filter by processed status</param>
    /// <param name="priority">Filter by priority</param>
    /// <param name="tags">Filter by tags (comma-separated)</param>
    /// <param name="searchText">Search text</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>List of capture items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<CaptureItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CaptureItemDto>>> GetCaptureItems(
        [FromQuery] CaptureType? type = null,
        [FromQuery] bool? isProcessed = null,
        [FromQuery] Priority? priority = null,
        [FromQuery] string? tags = null,
        [FromQuery] string? searchText = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetCaptureItemsQuery
            {
                Type = type,
                IsProcessed = isProcessed,
                Priority = priority,
                Tags = tags,
                SearchText = searchText,
                Page = page,
                PageSize = Math.Min(pageSize, 100)
            };

            var items = await _mediator.Send(query);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capture items");
            return StatusCode(500, "An error occurred while getting capture items");
        }
    }

    /// <summary>
    /// Get a specific capture item by ID
    /// </summary>
    /// <param name="id">Capture item ID</param>
    /// <returns>Capture item details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CaptureItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CaptureItemDto>> GetCaptureItem(Guid id)
    {
        try
        {
            var query = new GetCaptureItemQuery { Id = id };
            var item = await _mediator.Send(query);
            return Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capture item {CaptureItemId}", id);
            return StatusCode(500, "An error occurred while getting the capture item");
        }
    }

    /// <summary>
    /// Create a new capture item
    /// </summary>
    /// <param name="command">Capture item creation data</param>
    /// <returns>Created capture item</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CaptureItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CaptureItemDto>> CreateCaptureItem([FromBody] CreateCaptureItemCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var item = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetCaptureItem), new { id = item.Id }, item);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating capture item");
            return StatusCode(500, "An error occurred while creating the capture item");
        }
    }

    /// <summary>
    /// Process a capture item (mark as processed)
    /// </summary>
    /// <param name="id">Capture item ID</param>
    /// <param name="command">Processing data</param>
    /// <returns>Updated capture item</returns>
    [HttpPost("{id}/process")]
    [ProducesResponseType(typeof(CaptureItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CaptureItemDto>> ProcessCaptureItem(Guid id, [FromBody] ProcessCaptureItemCommand command)
    {
        try
        {
            command.Id = id;
            var item = await _mediator.Send(command);
            return Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing capture item {CaptureItemId}", id);
            return StatusCode(500, "An error occurred while processing the capture item");
        }
    }

    /// <summary>
    /// Convert a capture item to a task
    /// </summary>
    /// <param name="id">Capture item ID</param>
    /// <param name="command">Task conversion data</param>
    /// <returns>Created task</returns>
    [HttpPost("{id}/convert-to-task")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskDto>> ConvertToTaskk(Guid id, [FromBody] ConvertCaptureToTaskCommand command)
    {
        try
        {
            command.CaptureItemId = id;
            var task = await _mediator.Send(command);
            return CreatedAtAction("GetTask", "Tasks", new { id = task.Id }, task);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting capture item {CaptureItemId} to task", id);
            return StatusCode(500, "An error occurred while converting the capture item to a task");
        }
    }

    /// <summary>
    /// Update a capture item
    /// </summary>
    /// <param name="id">Capture item ID</param>
    /// <param name="command">Update data</param>
    /// <returns>Updated capture item</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CaptureItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CaptureItemDto>> UpdateCaptureItem(Guid id, [FromBody] UpdateCaptureItemCommand command)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest("Capture item ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var item = await _mediator.Send(command);
            return Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating capture item {CaptureItemId}", id);
            return StatusCode(500, "An error occurred while updating the capture item");
        }
    }

    /// <summary>
    /// Delete a capture item
    /// </summary>
    /// <param name="id">Capture item ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCaptureItem(Guid id)
    {
        try
        {
            var command = new DeleteCaptureItemCommand { Id = id };
            await _mediator.Send(command);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting capture item {CaptureItemId}", id);
            return StatusCode(500, "An error occurred while deleting the capture item");
        }
    }
}

// Placeholder command and query classes that would be implemented
public class GetCaptureItemsQuery : IRequest<List<CaptureItemDto>>
{
    public CaptureType? Type { get; set; }
    public bool? IsProcessed { get; set; }
    public Priority? Priority { get; set; }
    public string? Tags { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetCaptureItemQuery : IRequest<CaptureItemDto>
{
    public Guid Id { get; set; }
}

public class ProcessCaptureItemCommand : IRequest<CaptureItemDto>
{
    public Guid Id { get; set; }
    public string? ProcessingNotes { get; set; }
}

public class ConvertCaptureToTaskCommand : IRequest<TaskDto>
{
    public Guid CaptureItemId { get; set; }
    public string? TaskTitle { get; set; }
    public string? TaskDescription { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
}

public class UpdateCaptureItemCommand : IRequest<CaptureItemDto>
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public CaptureType Type { get; set; }
    public Priority Priority { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string? Context { get; set; }
    public int EnergyLevel { get; set; }
    public string? Mood { get; set; }
    public bool IsUrgent { get; set; }
}

public class DeleteCaptureItemCommand : IRequest
{
    public Guid Id { get; set; }
}