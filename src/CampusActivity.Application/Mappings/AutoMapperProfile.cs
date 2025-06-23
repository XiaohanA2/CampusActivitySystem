using AutoMapper;
using CampusActivity.Domain.Entities;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.Application.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        CreateMap<RegisterDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<Activity, ActivityDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.Creator.FullName))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.TagName).ToList()))
            .ForMember(dest => dest.IsRegistered, opt => opt.Ignore())
            .ForMember(dest => dest.RecommendationScore, opt => opt.Ignore());

        CreateMap<CreateActivityDto, Activity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentParticipants, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ActivityStatus.Published))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<UpdateActivityDto, Activity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<ActivityCategory, ActivityCategoryDto>()
            .ForMember(dest => dest.ActivityCount, opt => opt.MapFrom(src => src.Activities.Count));

        CreateMap<ActivityCategoryDto, ActivityCategory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<ActivityRegistration, ActivityRegistrationDto>()
            .ForMember(dest => dest.ActivityTitle, opt => opt.MapFrom(src => src.Activity.Title))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

        CreateMap<ActivityTag, string>()
            .ConvertUsing(src => src.TagName);

        CreateMap<string, ActivityTag>()
            .ForMember(dest => dest.TagName, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ActivityId, opt => opt.Ignore())
            .ForMember(dest => dest.Color, opt => opt.Ignore());

        // 日程表映射
        CreateMap<ScheduleItem, ScheduleItemDto>()
            .ForMember(dest => dest.ActivityTitle, opt => opt.Ignore())
            .ForMember(dest => dest.ActivityLocation, opt => opt.Ignore());

        CreateMap<CreateScheduleItemDto, ScheduleItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<UpdateScheduleItemDto, ScheduleItem>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
} 