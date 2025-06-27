using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public interface IAdminService
{
    // 用户管理
    Task<PagedResult<UserDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, string? search = null, UserRole? role = null);
    Task<bool> UpdateUserStatusAsync(int id, bool isActive);
    Task<bool> ResetUserPasswordAsync(int id);
    Task<bool> DeleteUserAsync(int id);

    // 活动管理
    Task<PagedResult<AdminActivityDto>> GetAllActivitiesAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, string? status = null);
    Task<AdminActivityDto?> GetActivityByIdAsync(int id);
    Task<bool> UpdateActivityAsync(AdminActivityDto activity);
    Task<bool> CreateActivityAsync(AdminActivityDto activity);
    Task<bool> ForceDeleteActivityAsync(int id);
    Task<bool> DeleteActivityAsync(int activityId);
    Task<IEnumerable<ActivityCategoryDto>> GetCategoriesAsync();

    // 系统统计
    Task<SystemStatisticsDto> GetSystemStatisticsAsync();
    Task SeedActivityImagesAsync();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class SystemStatisticsDto
{
    public BasicStatistics BasicStatistics { get; set; } = new();
    public RecentStatistics RecentStatistics { get; set; } = new();
    public List<RoleDistribution> UserRoleDistribution { get; set; } = new();
    public List<PopularActivity> PopularActivities { get; set; } = new();
    public List<CategoryStatistic> CategoryStatistics { get; set; } = new();
    public List<DailyRegistration> DailyRegistrations { get; set; } = new();
}

public class BasicStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalActivities { get; set; }
    public int PublishedActivities { get; set; }
    public int TotalRegistrations { get; set; }
}

public class RecentStatistics
{
    public int NewUsersLast30Days { get; set; }
    public int NewActivitiesLast30Days { get; set; }
    public int NewRegistrationsLast30Days { get; set; }
}

public class RoleDistribution
{
    public string Role { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PopularActivity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int RegistrationCount { get; set; }
}

public class CategoryStatistic
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyRegistration
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
} 