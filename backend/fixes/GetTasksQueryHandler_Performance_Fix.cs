// PERFORMANCE FIX: Replace GetTasksQueryHandler.cs Handle method with this optimized version
// This eliminates the N+1 query pattern and reduces memory usage

public async Task<List<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
{
    if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
    {
        throw new UnauthorizedAccessException("User must be authenticated to get tasks.");
    }

    // Build the base query with direct projection to DTO
    // This eliminates the N+1 pattern by calculating subtask counts in the database
    var query = _context.Tasks
        .Where(t => t.UserId == _currentUserService.UserId.Value);

    // Apply filters (same as before)
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
            .Select(t => t.Trim().ToLower()).ToList();
        
        // Optimized tag search using ANY operator for better performance
        query = query.Where(t => tags.Any(tag => t.Tags.ToLower().Contains(tag)));
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

    // CRITICAL PERFORMANCE FIX: Project directly to DTO with subtask counts calculated in database
    var taskDtos = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
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
            // Calculate subtask counts efficiently in the database (eliminates N+1 pattern)
            SubTaskCount = t.SubTasks.Count(),
            CompletedSubTaskCount = t.SubTasks.Count(st => st.Status == TaskStatus.Completed)
        })
        .ToListAsync(cancellationToken);

    return taskDtos;
}

// ALTERNATIVE: If you need to keep AutoMapper, use this approach instead:
/*
public async Task<List<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
{
    // ... same filtering logic as above ...

    // Load tasks with explicit include and track subtask counts separately
    var tasks = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(t => new { 
            Task = t, 
            SubTaskCount = t.SubTasks.Count(),
            CompletedSubTaskCount = t.SubTasks.Count(st => st.Status == TaskStatus.Completed)
        })
        .ToListAsync(cancellationToken);

    var taskDtos = tasks.Select(x => {
        var dto = _mapper.Map<TaskDto>(x.Task);
        dto.SubTaskCount = x.SubTaskCount;
        dto.CompletedSubTaskCount = x.CompletedSubTaskCount;
        return dto;
    }).ToList();

    return taskDtos;
}
*/