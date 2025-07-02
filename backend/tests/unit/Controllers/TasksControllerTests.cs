using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MediatR;
using AdhdProductivitySystem.Api.Controllers;
using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;
using AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;
using AdhdProductivitySystem.Domain.Enums;

namespace AdhdProductivitySystem.Tests.Unit.Controllers;

public class TasksControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TasksController>> _mockLogger;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TasksController>>();
        _controller = new TasksController(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetTasks_ReturnsOkResult_WithTaskList()
    {
        // Arrange
        var expectedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Task 1",
                Priority = Priority.High,
                Status = TaskStatus.Todo
            },
            new TaskDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Task 2",
                Priority = Priority.Medium,
                Status = TaskStatus.InProgress
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTasks);

        // Act
        var result = await _controller.GetTasks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsType<List<TaskDto>>(okResult.Value);
        Assert.Equal(2, tasks.Count);
        Assert.Equal("Test Task 1", tasks[0].Title);
        Assert.Equal("Test Task 2", tasks[1].Title);
    }

    [Fact]
    public async Task GetTasks_WithFilters_PassesCorrectQueryParameters()
    {
        // Arrange
        var status = TaskStatus.InProgress;
        var priority = Priority.High;
        var dueDateFrom = DateTime.UtcNow;
        var dueDateTo = DateTime.UtcNow.AddDays(7);
        var tags = "urgent,work";
        var searchText = "project";
        var page = 2;
        var pageSize = 20;
        var sortBy = "Priority";
        var sortDescending = false;

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskDto>());

        // Act
        await _controller.GetTasks(status, priority, dueDateFrom, dueDateTo, tags, 
            searchText, true, page, pageSize, sortBy, sortDescending);

        // Assert
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTasksQuery>(q => 
                q.Status == status &&
                q.Priority == priority &&
                q.DueDateFrom == dueDateFrom &&
                q.DueDateTo == dueDateTo &&
                q.Tags == tags &&
                q.SearchText == searchText &&
                q.Page == page &&
                q.PageSize == pageSize &&
                q.SortBy == sortBy &&
                q.SortDescending == sortDescending),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTasks_LimitsPageSize_ToMaximum100()
    {
        // Arrange
        var oversizedPageSize = 150;
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskDto>());

        // Act
        await _controller.GetTasks(pageSize: oversizedPageSize);

        // Assert
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTasksQuery>(q => q.PageSize == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTasks_ReturnsUnauthorized_WhenUnauthorizedAccessException()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetTasks();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTasks_ReturnsInternalServerError_WhenGeneralException()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTasks();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ReturnsCreatedResult_WithValidCommand()
    {
        // Arrange
        var command = new CreateTaskCommand
        {
            Title = "New Task",
            Description = "Task description",
            Priority = Priority.Medium,
            EstimatedMinutes = 60
        };

        var createdTask = new TaskDto
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Description = command.Description,
            Priority = command.Priority,
            EstimatedMinutes = command.EstimatedMinutes,
            Status = TaskStatus.Todo
        };

        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        // Act
        var result = await _controller.CreateTask(command);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var task = Assert.IsType<TaskDto>(createdResult.Value);
        Assert.Equal(createdTask.Id, task.Id);
        Assert.Equal(createdTask.Title, task.Title);
        Assert.Equal("GetTask", createdResult.ActionName);
        Assert.Equal(createdTask.Id, ((dynamic)createdResult.RouteValues!).id);
    }

    [Fact]
    public async Task CreateTask_ReturnsBadRequest_WhenArgumentException()
    {
        // Arrange
        var command = new CreateTaskCommand { Title = "" }; // Invalid title
        
        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Title is required"));

        // Act
        var result = await _controller.CreateTask(command);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Title is required", badRequestResult.Value);
    }

    [Fact]
    public async Task GetTask_ReturnsTask_WhenTaskExists()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var expectedTask = new TaskDto
        {
            Id = taskId,
            Title = "Existing Task",
            Priority = Priority.Low,
            Status = TaskStatus.Todo
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskDto> { expectedTask });

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var task = Assert.IsType<TaskDto>(okResult.Value);
        Assert.Equal(taskId, task.Id);
        Assert.Equal("Existing Task", task.Title);
    }

    [Fact]
    public async Task GetTask_ReturnsNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskDto>());

        // Act
        var result = await _controller.GetTask(taskId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}