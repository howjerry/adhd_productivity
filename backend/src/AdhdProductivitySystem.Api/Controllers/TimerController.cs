using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdhdProductivitySystem.Api.Controllers;

/// <summary>
/// Controller for managing timer sessions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TimerController> _logger;

    public TimerController(IMediator mediator, ILogger<TimerController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get timer sessions for the current user
    /// </summary>
    /// <param name="type">Filter by timer type</param>
    /// <param name="status">Filter by status</param>
    /// <param name="taskId">Filter by associated task</param>
    /// <param name="dateFrom">Filter by date range</param>
    /// <param name="dateTo">Filter by date range</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>List of timer sessions</returns>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<TimerSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TimerSessionDto>>> GetTimerSessions(
        [FromQuery] TimerType? type = null,
        [FromQuery] TimerStatus? status = null,
        [FromQuery] Guid? taskId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetTimerSessionsQuery
            {
                Type = type,
                Status = status,
                TaskId = taskId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = Math.Min(pageSize, 100)
            };

            var sessions = await _mediator.Send(query);
            return Ok(sessions);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timer sessions");
            return StatusCode(500, "An error occurred while getting timer sessions");
        }
    }

    /// <summary>
    /// Get a specific timer session by ID
    /// </summary>
    /// <param name="id">Timer session ID</param>
    /// <returns>Timer session details</returns>
    [HttpGet("sessions/{id}")]
    [ProducesResponseType(typeof(TimerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerSessionDto>> GetTimerSession(Guid id)
    {
        try
        {
            var query = new GetTimerSessionQuery { Id = id };
            var session = await _mediator.Send(query);
            return Ok(session);
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
            _logger.LogError(ex, "Error getting timer session {TimerSessionId}", id);
            return StatusCode(500, "An error occurred while getting the timer session");
        }
    }

    /// <summary>
    /// Start a new timer session
    /// </summary>
    /// <param name="command">Timer session start data</param>
    /// <returns>Started timer session</returns>
    [HttpPost("sessions/start")]
    [ProducesResponseType(typeof(TimerSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerSessionDto>> StartTimerSession([FromBody] StartTimerSessionCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetTimerSession), new { id = session.Id }, session);
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
            _logger.LogError(ex, "Error starting timer session");
            return StatusCode(500, "An error occurred while starting the timer session");
        }
    }

    /// <summary>
    /// Pause a running timer session
    /// </summary>
    /// <param name="id">Timer session ID</param>
    /// <returns>Updated timer session</returns>
    [HttpPost("sessions/{id}/pause")]
    [ProducesResponseType(typeof(TimerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerSessionDto>> PauseTimerSession(Guid id)
    {
        try
        {
            var command = new PauseTimerSessionCommand { Id = id };
            var session = await _mediator.Send(command);
            return Ok(session);
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
            _logger.LogError(ex, "Error pausing timer session {TimerSessionId}", id);
            return StatusCode(500, "An error occurred while pausing the timer session");
        }
    }

    /// <summary>
    /// Resume a paused timer session
    /// </summary>
    /// <param name="id">Timer session ID</param>
    /// <returns>Updated timer session</returns>
    [HttpPost("sessions/{id}/resume")]
    [ProducesResponseType(typeof(TimerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerSessionDto>> ResumeTimerSession(Guid id)
    {
        try
        {
            var command = new ResumeTimerSessionCommand { Id = id };
            var session = await _mediator.Send(command);
            return Ok(session);
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
            _logger.LogError(ex, "Error resuming timer session {TimerSessionId}", id);
            return StatusCode(500, "An error occurred while resuming the timer session");
        }
    }

    /// <summary>
    /// Stop a timer session
    /// </summary>
    /// <param name="id">Timer session ID</param>
    /// <param name="command">Stop session data</param>
    /// <returns>Updated timer session</returns>
    [HttpPost("sessions/{id}/stop")]
    [ProducesResponseType(typeof(TimerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerSessionDto>> StopTimerSession(Guid id, [FromBody] StopTimerSessionCommand command)
    {
        try
        {
            command.Id = id;
            var session = await _mediator.Send(command);
            return Ok(session);
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
            _logger.LogError(ex, "Error stopping timer session {TimerSessionId}", id);
            return StatusCode(500, "An error occurred while stopping the timer session");
        }
    }

    /// <summary>
    /// Complete a timer session
    /// </summary>
    /// <param name="id">Timer session ID</param>
    /// <param name="command">Completion data</param>
    /// <returns>Updated timer session</returns>
    [HttpPost("sessions/{id}/complete")]
    [ProducesResponseType(typeof(TimerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerSessionDto>> CompleteTimerSession(Guid id, [FromBody] CompleteTimerSessionCommand command)
    {
        try
        {
            command.Id = id;
            var session = await _mediator.Send(command);
            return Ok(session);
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
            _logger.LogError(ex, "Error completing timer session {TimerSessionId}", id);
            return StatusCode(500, "An error occurred while completing the timer session");
        }
    }

    /// <summary>
    /// Get timer statistics for the current user
    /// </summary>
    /// <param name="dateFrom">Date range start</param>
    /// <param name="dateTo">Date range end</param>
    /// <returns>Timer statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TimerStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimerStatisticsDto>> GetTimerStatistics(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var query = new GetTimerStatisticsQuery
            {
                DateFrom = dateFrom ?? DateTime.UtcNow.AddDays(-30),
                DateTo = dateTo ?? DateTime.UtcNow
            };

            var statistics = await _mediator.Send(query);
            return Ok(statistics);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timer statistics");
            return StatusCode(500, "An error occurred while getting timer statistics");
        }
    }
}

// Placeholder command and query classes that would be implemented
public class GetTimerSessionsQuery : IRequest<List<TimerSessionDto>>
{
    public TimerType? Type { get; set; }
    public TimerStatus? Status { get; set; }
    public Guid? TaskId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetTimerSessionQuery : IRequest<TimerSessionDto>
{
    public Guid Id { get; set; }
}

public class StartTimerSessionCommand : IRequest<TimerSessionDto>
{
    public TimerType Type { get; set; } = TimerType.Pomodoro;
    public int PlannedMinutes { get; set; } = 25;
    public Guid? TaskId { get; set; }
    public string Tags { get; set; } = string.Empty;
    public int? StartEnergyLevel { get; set; }
}

public class PauseTimerSessionCommand : IRequest<TimerSessionDto>
{
    public Guid Id { get; set; }
}

public class ResumeTimerSessionCommand : IRequest<TimerSessionDto>
{
    public Guid Id { get; set; }
}

public class StopTimerSessionCommand : IRequest<TimerSessionDto>
{
    public Guid Id { get; set; }
    public string? Notes { get; set; }
    public int? EndEnergyLevel { get; set; }
}

public class CompleteTimerSessionCommand : IRequest<TimerSessionDto>
{
    public Guid Id { get; set; }
    public string? Notes { get; set; }
    public string? Accomplishments { get; set; }
    public string? Challenges { get; set; }
    public int? FocusLevel { get; set; }
    public int? EndEnergyLevel { get; set; }
    public int Interruptions { get; set; } = 0;
}

public class GetTimerStatisticsQuery : IRequest<TimerStatisticsDto>
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class TimerStatisticsDto
{
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalMinutes { get; set; }
    public double AverageFocusLevel { get; set; }
    public int TotalInterruptions { get; set; }
    public Dictionary<TimerType, int> SessionsByType { get; set; } = new();
    public Dictionary<string, int> SessionsByDay { get; set; } = new();
}