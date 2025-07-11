using AdhdProductivitySystem.Application.Common.DTOs;
using AdhdProductivitySystem.Domain.Entities;
using AutoMapper;

namespace AdhdProductivitySystem.Application.Mappings;

/// <summary>
/// AutoMapper profile for entity to DTO mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Task mappings
        CreateMap<TaskItem, TaskDto>()
            .ForMember(dest => dest.SubTaskCount, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedSubTaskCount, opt => opt.Ignore());

        CreateMap<TaskDto, TaskItem>()
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.ParentTask, opt => opt.Ignore())
            .ForMember(dest => dest.SubTasks, opt => opt.Ignore())
            .ForMember(dest => dest.TimerSessions, opt => opt.Ignore());

        // CaptureItem mappings
        CreateMap<CaptureItem, CaptureItemDto>()
            .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => src.Task != null ? src.Task.Title : null));

        CreateMap<CaptureItemDto, CaptureItem>()
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Task, opt => opt.Ignore());

        // TimerSession mappings
        CreateMap<TimerSession, TimerSessionDto>()
            .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => src.Task != null ? src.Task.Title : null));

        CreateMap<TimerSessionDto, TimerSession>()
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Task, opt => opt.Ignore());

        // TimeBlock mappings
        CreateMap<TimeBlock, TimeBlockDto>()
            .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => src.Task != null ? src.Task.Title : null));

        CreateMap<TimeBlockDto, TimeBlock>()
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Task, opt => opt.Ignore());

        // UserProgress mappings
        CreateMap<UserProgress, UserProgressDto>();
        CreateMap<UserProgressDto, UserProgress>()
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .ForMember(dest => dest.CaptureItems, opt => opt.Ignore())
            .ForMember(dest => dest.TimeBlocks, opt => opt.Ignore())
            .ForMember(dest => dest.ProgressRecords, opt => opt.Ignore())
            .ForMember(dest => dest.TimerSessions, opt => opt.Ignore());
    }
}