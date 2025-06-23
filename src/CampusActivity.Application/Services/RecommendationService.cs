using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;

// 注意：这里我们假设C++/CLI库已经正确编译并可以引用
// 在实际项目中，您需要添加对CampusActivity.NativeLib的引用
// using CampusActivity.NativeLib;

namespace CampusActivity.Application.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RecommendationService> _logger;
    private readonly IDistributedCache _cache;

    public RecommendationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RecommendationService> logger,
        IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<ActivityDto>> GetRecommendedActivitiesAsync(int userId, int count = 10)
    {
        try
        {
            var cacheKey = string.Format(AppConstants.CacheKeys.RecommendedActivities, userId);
            var cachedRecommendations = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRecommendations))
            {
                var cached = JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(cachedRecommendations);
                return cached?.Take(count) ?? Enumerable.Empty<ActivityDto>();
            }

            // 获取用户偏好
            var userPreferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
            
            // 获取可推荐的活动
            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow);

            // 获取用户已报名的活动
            var userRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.UserId == userId && r.Status == RegistrationStatus.Registered);
            var registeredActivityIds = userRegistrations.Select(r => r.ActivityId).ToList();

            var recommendedActivities = new List<ActivityDto>();

            // 由于C++/CLI模块在开发环境中可能还没有完全配置，我们先使用C#实现基础推荐逻辑
            // 在生产环境中，这里应该调用C++/CLI推荐引擎
            var recommendations = await CalculateBasicRecommendations(userId, activities, userPreferences, registeredActivityIds);
            
            recommendedActivities = recommendations.Take(count).ToList();

            // 缓存结果
            var serializedRecommendations = JsonSerializer.Serialize(recommendedActivities);
            await _cache.SetStringAsync(cacheKey, serializedRecommendations, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Medium
            });

            return recommendedActivities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推荐活动失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<ActivityDto>();
        }
    }

    public async Task<IEnumerable<ActivityDto>> GetCollaborativeRecommendationsAsync(int userId, int count = 10)
    {
        try
        {
            // 协同过滤推荐实现
            var userRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.UserId == userId && r.Status == RegistrationStatus.Registered);
            var userActivityIds = userRegistrations.Select(r => r.ActivityId).ToHashSet();

            // 找到有相似活动偏好的用户
            var allRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.Status == RegistrationStatus.Registered);
            
            var similarUsers = FindSimilarUsers(userId, userActivityIds, allRegistrations);
            
            // 基于相似用户推荐活动
            var recommendedActivityIds = new List<int>();
            foreach (var similarUserId in similarUsers.Take(10))
            {
                var similarUserRegistrations = allRegistrations.Where(r => r.UserId == similarUserId.Key).ToList();
                foreach (var registration in similarUserRegistrations)
                {
                    if (!userActivityIds.Contains(registration.ActivityId))
                    {
                        recommendedActivityIds.Add(registration.ActivityId);
                    }
                }
            }

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                recommendedActivityIds.Contains(a.Id) && 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow);

            var activityDtos = _mapper.Map<IEnumerable<ActivityDto>>(activities);
            return activityDtos.Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "协同过滤推荐失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<ActivityDto>();
        }
    }

    public async Task<IEnumerable<ActivityDto>> GetContentBasedRecommendationsAsync(int userId, int count = 10)
    {
        try
        {
            // 基于内容的推荐
            var userPreferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
            var preferenceDict = userPreferences.ToDictionary(p => p.CategoryId, p => p.Weight);

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow);

            var scoredActivities = activities.Select(a => new
            {
                Activity = a,
                Score = preferenceDict.ContainsKey(a.CategoryId) ? preferenceDict[a.CategoryId] : 0.0
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(count)
            .Select(x => x.Activity);

            return _mapper.Map<IEnumerable<ActivityDto>>(scoredActivities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "基于内容推荐失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<ActivityDto>();
        }
    }

    public async Task UpdateUserPreferencesAsync(int userId, int categoryId, double weight)
    {
        var preference = await _unitOfWork.UserActivityPreferences.FirstOrDefaultAsync(p => 
            p.UserId == userId && p.CategoryId == categoryId);

        if (preference == null)
        {
            preference = new UserActivityPreference
            {
                UserId = userId,
                CategoryId = categoryId,
                Weight = weight,
                LastUpdated = DateTime.UtcNow
            };
            await _unitOfWork.UserActivityPreferences.AddAsync(preference);
        }
        else
        {
            preference.Weight = weight;
            preference.LastUpdated = DateTime.UtcNow;
            await _unitOfWork.UserActivityPreferences.UpdateAsync(preference);
        }

        await _unitOfWork.SaveChangesAsync();

        // 清除推荐缓存
        var cacheKey = string.Format(AppConstants.CacheKeys.RecommendedActivities, userId);
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task<IEnumerable<UserActivityPreferenceDto>> GetUserPreferencesAsync(int userId)
    {
        var preferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
        
        var result = new List<UserActivityPreferenceDto>();
        foreach (var preference in preferences)
        {
            var category = await _unitOfWork.ActivityCategories.GetByIdAsync(preference.CategoryId);
            result.Add(new UserActivityPreferenceDto
            {
                UserId = preference.UserId,
                CategoryId = preference.CategoryId,
                CategoryName = category?.Name ?? "未知分类",
                Weight = preference.Weight,
                LastUpdated = preference.LastUpdated
            });
        }

        return result;
    }

    public async Task RecalculateRecommendationsAsync(int userId)
    {
        // 清除缓存，强制重新计算
        var cacheKey = string.Format(AppConstants.CacheKeys.RecommendedActivities, userId);
        await _cache.RemoveAsync(cacheKey);

        // 预热缓存 - 不需要await，因为返回的是IEnumerable
        _ = GetRecommendedActivitiesAsync(userId);
    }

    private async Task<IEnumerable<ActivityDto>> CalculateBasicRecommendations(
        int userId, 
        IEnumerable<Activity> activities, 
        IEnumerable<UserActivityPreference> userPreferences,
        List<int> registeredActivityIds)
    {
        var preferenceDict = userPreferences.ToDictionary(p => p.CategoryId, p => p.Weight);
        
        var scoredActivities = new List<(Activity activity, double score)>();

        foreach (var activity in activities)
        {
            if (registeredActivityIds.Contains(activity.Id))
                continue;

            double score = 0.0;

            // 基于用户偏好的分数
            if (preferenceDict.ContainsKey(activity.CategoryId))
            {
                score += preferenceDict[activity.CategoryId] * 0.4;
            }

            // 活动热度分数
            double popularityScore = CalculatePopularityScore(activity);
            score += popularityScore * 0.3;

            // 时间衰减因子
            double timeDecay = CalculateTimeDecayFactor(activity.StartTime);
            score *= timeDecay;

            if (score > AppConstants.Recommendation.MinRecommendationScore)
            {
                scoredActivities.Add((activity, score));
            }
        }

        var topActivities = scoredActivities
            .OrderByDescending(x => x.score)
            .Take(AppConstants.Recommendation.MaxRecommendedActivities)
            .Select(x => x.activity);

        return _mapper.Map<IEnumerable<ActivityDto>>(topActivities);
    }

    private Dictionary<int, double> FindSimilarUsers(int userId, HashSet<int> userActivityIds, IEnumerable<ActivityRegistration> allRegistrations)
    {
        var userActivityCounts = new Dictionary<int, HashSet<int>>();
        
        foreach (var registration in allRegistrations)
        {
            if (registration.UserId == userId) continue;
            
            if (!userActivityCounts.ContainsKey(registration.UserId))
                userActivityCounts[registration.UserId] = new HashSet<int>();
            
            userActivityCounts[registration.UserId].Add(registration.ActivityId);
        }

        var similarities = new Dictionary<int, double>();
        
        foreach (var kvp in userActivityCounts)
        {
            var otherUserActivities = kvp.Value;
            var intersection = userActivityIds.Intersect(otherUserActivities).Count();
            var union = userActivityIds.Union(otherUserActivities).Count();
            
            if (union > 0)
            {
                double similarity = (double)intersection / union; // Jaccard相似度
                if (similarity > 0.1)
                {
                    similarities[kvp.Key] = similarity;
                }
            }
        }

        return similarities.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
    }

    private double CalculatePopularityScore(Activity activity)
    {
        double registrationRatio = (double)activity.CurrentParticipants / activity.MaxParticipants;
        
        // 使用S型曲线，避免过度偏向满员活动
        return 1.0 / (1.0 + Math.Exp(-10.0 * (registrationRatio - 0.5)));
    }

    private double CalculateTimeDecayFactor(DateTime activityTime)
    {
        var timeDiff = activityTime - DateTime.UtcNow;
        
        if (timeDiff.TotalDays < 0)
            return 0.0;
        
        double days = timeDiff.TotalDays;
        
        if (days <= 7)
            return 1.0;
        else if (days <= 30)
            return 0.8;
        else if (days <= 90)
            return 0.6;
        else
            return 0.4;
    }
} 