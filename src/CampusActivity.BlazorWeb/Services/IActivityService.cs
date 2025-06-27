using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public interface IActivityService
{
    Task<PagedResultDto<ActivityDto>?> GetActivitiesAsync(ActivitySearchDto searchDto);
    Task<ActivityDto?> GetActivityByIdAsync(int id);
    Task<ActivityDto?> CreateActivityAsync(CreateActivityDto createDto);
    Task<ActivityDto?> UpdateActivityAsync(int id, UpdateActivityDto updateDto);
    Task<bool> DeleteActivityAsync(int id);
    Task<bool> RegisterForActivityAsync(int activityId, string? note = null);
    Task<bool> CancelRegistrationAsync(int activityId);
    Task<IEnumerable<ActivityRegistrationDto>?> GetActivityRegistrationsAsync(int activityId);
    Task<IEnumerable<ActivityCategoryDto>?> GetCategoriesAsync();
    Task<IEnumerable<ActivityDto>?> GetPopularActivitiesAsync(int count = 10);
} 