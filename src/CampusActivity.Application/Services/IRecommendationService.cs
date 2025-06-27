using CampusActivity.Shared.DTOs;

namespace CampusActivity.Application.Services;

public interface IRecommendationService
{
    Task<IEnumerable<ActivityDto>> GetRecommendedActivitiesAsync(int userId, int count = 10);
    Task<IEnumerable<ActivityDto>> GetCollaborativeRecommendationsAsync(int userId, int count = 10);
    Task<IEnumerable<ActivityDto>> GetContentBasedRecommendationsAsync(int userId, int count = 10);
    Task<IEnumerable<UserActivityPreferenceDto>> GetUserPreferencesAsync(int userId);
    Task UpdateUserPreferencesAsync(int userId, int categoryId, double weight);
    Task RecalculateRecommendationsAsync(int userId);
} 