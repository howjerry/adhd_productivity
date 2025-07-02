using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AdhdProductivitySystem.Application.Features.Tasks.Queries.GetTasks;

/// <summary>
/// Handler for getting tasks
/// </summary>
public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<TaskDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTasksQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<List<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to get tasks.");
        }

        var query = _context.Tasks
            .Include(t => t.SubTasks)
            .Where(t => t.UserId == _currentUserService.UserId.Value);

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        if (request.DueDateFrom.HasValue)
        {
            query = query.Where(t => t.DueDate >= request.DueDateFrom.Value);
        }

        if (request.DueDateTo.HasValue)
        {
            query = query.Where(t => t.DueDate <= request.DueDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            var tags = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower());
            
            foreach (var tag in tags)
            {
                query = query.Where(t => t.Tags.ToLower().Contains(tag));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchText) || 
                                   (t.Description != null && t.Description.ToLower().Contains(searchText)));
        }

        if (!request.IncludeSubTasks)
        {
            query = query.Where(t => t.ParentTaskId == null);
        }

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "priority" => request.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "duedate" => request.SortDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "status" => request.SortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "updatedat" => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            _ => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };

        // Apply pagination
        var tasks = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var taskDtos = new List<TaskDto>();
        foreach (var task in tasks)
        {
            var taskDto = _mapper.Map<TaskDto>(task);
            taskDto.SubTaskCount = task.SubTasks.Count;
            taskDto.CompletedSubTaskCount = task.SubTasks.Count(st => st.Status == TaskStatus.Completed);
            taskDtos.Add(taskDto);
        }

        return taskDtos;
    }
}