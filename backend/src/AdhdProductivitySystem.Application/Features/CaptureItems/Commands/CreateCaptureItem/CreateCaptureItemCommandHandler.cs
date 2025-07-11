using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AutoMapper;
using MediatR;

namespace AdhdProductivitySystem.Application.Features.CaptureItems.Commands.CreateCaptureItem;

/// <summary>
/// Handler for creating a new capture item
/// </summary>
public class CreateCaptureItemCommandHandler : IRequestHandler<CreateCaptureItemCommand, CaptureItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateCaptureItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<CaptureItemDto> Handle(CreateCaptureItemCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to create capture items.");
        }

        var captureItem = new CaptureItem
        {
            Content = request.Content,
            Type = request.Type,
            Priority = request.Priority,
            Tags = request.Tags,
            Context = request.Context,
            EnergyLevel = request.EnergyLevel,
            Mood = request.Mood,
            IsUrgent = request.IsUrgent,
            UserId = _currentUserService.UserId.Value,
            CreatedBy = _currentUserService.UserEmail
        };

        _context.CaptureItems.Add(captureItem);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CaptureItemDto>(captureItem);
    }
}