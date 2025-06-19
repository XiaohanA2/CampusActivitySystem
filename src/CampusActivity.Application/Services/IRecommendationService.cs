using CampusActivity.Shared.DTOs;

namespace CampusActivity.Application.Services;

public interface IRecommendationService
{
    Task<IEnumerable<ActivityDto>> GetRecommendedActivitiesAsync(int userId, int count = 10);
    Task<IEnumerable<ActivityDto>> GetCollaborativeRecommendationsAsync(int userId, int count = 10);
    Task<IEnumerable<ActivityDto>> GetContentBasedRecommendationsAsync(int userId, int count = 10);
    Task UpdateUserPreferencesAsync(int userId, int categoryId, double weight);
    Task<IEnumerable<UserActivityPreferenceDto>> GetUserPreferencesAsync(int userId);
    Task RecalculateRecommendationsAsync(int userId);
}

public class UserActivityPreferenceDto
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public DateTime LastUpdated { get; set; }
} 