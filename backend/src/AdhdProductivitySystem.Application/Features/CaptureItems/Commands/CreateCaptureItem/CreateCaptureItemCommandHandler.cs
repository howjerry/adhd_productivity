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
    private readonly ICaptureItemRepository _captureItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateCaptureItemCommandHandler(
        ICaptureItemRepository captureItemRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _captureItemRepository = captureItemRepository;
        _unitOfWork = unitOfWork;
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

        _captureItemRepository.Add(captureItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 使用 Repository 的方法取得創建的項目
        var captureItemDto = await _captureItemRepository.GetCaptureItemByIdAsync(
            _currentUserService.UserId.Value,
            captureItem.Id,
            cancellationToken);

        if (captureItemDto == null)
        {
            throw new InvalidOperationException("Failed to retrieve created capture item.");
        }

        return captureItemDto;
    }
}