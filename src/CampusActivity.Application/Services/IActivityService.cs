using CampusActivity.Shared.DTOs;

namespace CampusActivity.Application.Services;

public interface IActivityService
{
    Task<PagedResultDto<ActivityDto>> GetActivitiesAsync(ActivitySearchDto searchDto);
    Task<ActivityDto?> GetActivityByIdAsync(int id, int? currentUserId = null);
    Task<ActivityDto> CreateActivityAsync(CreateActivityDto createDto, int createdBy);
    Task<ActivityDto> UpdateActivityAsync(int id, UpdateActivityDto updateDto, int updatedBy);
    Task<bool> DeleteActivityAsync(int id);
    Task<bool> RegisterForActivityAsync(int activityId, int userId, string? note = null);
    Task<bool> CancelRegistrationAsync(int activityId, int userId);
    Task<IEnumerable<ActivityRegistrationDto>> GetActivityRegistrationsAsync(int activityId);
    Task<IEnumerable<ActivityDto>> GetUserRegisteredActivitiesAsync(int userId);
    Task<IEnumerable<ActivityCategoryDto>> GetCategoriesAsync();
    Task<ActivityCategoryDto> CreateCategoryAsync(ActivityCategoryDto categoryDto);
    Task<bool> UpdateActivityStatusAsync(int activityId, ActivityStatus status);
    Task<IEnumerable<ActivityDto>> GetPopularActivitiesAsync(int count = 10);
} 