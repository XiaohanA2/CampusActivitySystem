using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public interface IRecommendationService
{
    Task<IEnumerable<ActivityDto>?> GetRecommendationsAsync(int count = 10);
    Task<IEnumerable<ActivityDto>?> GetCollaborativeRecommendationsAsync(int count = 10);
    Task<IEnumerable<ActivityDto>?> GetContentBasedRecommendationsAsync(int count = 10);
    Task<bool> RecalculateRecommendationsAsync();
} 