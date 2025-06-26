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
            string? cachedRecommendations = null;
            
            try
            {
                cachedRecommendations = await _cache.GetStringAsync(cacheKey);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "缓存访问失败，直接计算推荐");
            }

            if (!string.IsNullOrEmpty(cachedRecommendations))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(cachedRecommendations);
                    if (cached != null && cached.Any())
                    {
                        return cached.Take(count);
                    }
                }
                catch (Exception deserializeEx)
                {
                    _logger.LogWarning(deserializeEx, "缓存数据反序列化失败，重新计算");
                }
            }

            // 验证用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，用户ID: {UserId}", userId);
                return Enumerable.Empty<ActivityDto>();
            }

            // 获取用户偏好
            var userPreferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
            
            // 获取可推荐的活动
            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow &&
                a.StartTime > DateTime.UtcNow);

            if (!activities.Any())
            {
                _logger.LogInformation("没有可推荐的活动");
                return Enumerable.Empty<ActivityDto>();
            }

            // 获取用户已报名的活动
            var userRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.UserId == userId && r.Status == RegistrationStatus.Registered);
            var registeredActivityIds = userRegistrations.Select(r => r.ActivityId).ToHashSet();

            // 过滤掉已报名的活动
            var availableActivities = activities.Where(a => !registeredActivityIds.Contains(a.Id));

            List<ActivityDto> recommendedActivities;

            // 如果用户有偏好设置，使用个性化推荐
            if (userPreferences.Any())
            {
                var recommendations = await CalculateBasicRecommendations(userId, availableActivities, userPreferences, registeredActivityIds.ToList());
                recommendedActivities = recommendations.Take(count).ToList();
            }
            else
            {
                // 如果没有偏好设置，推荐热门活动
                var defaultRecommendations = availableActivities
                    .OrderByDescending(a => a.CurrentParticipants)
                    .ThenBy(a => Math.Abs((a.StartTime - DateTime.UtcNow).TotalDays))
                    .Take(count);
                recommendedActivities = _mapper.Map<List<ActivityDto>>(defaultRecommendations);
            }

            // 缓存结果
            try
            {
                var serializedRecommendations = JsonSerializer.Serialize(recommendedActivities, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await _cache.SetStringAsync(cacheKey, serializedRecommendations, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Medium
                });
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "缓存设置失败");
            }

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
            // 验证用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，用户ID: {UserId}", userId);
                return Enumerable.Empty<ActivityDto>();
            }

            // 协同过滤推荐实现
            var userRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.UserId == userId && r.Status == RegistrationStatus.Registered);
            var userActivityIds = userRegistrations.Select(r => r.ActivityId).ToHashSet();

            // 如果用户没有报名过任何活动，返回热门活动
            if (!userActivityIds.Any())
            {
                _logger.LogInformation("用户没有报名记录，返回热门活动，用户ID: {UserId}", userId);
                var popularActivities = await _unitOfWork.Activities.FindAsync(a => 
                    a.Status == ActivityStatus.Published && 
                    a.RegistrationDeadline > DateTime.UtcNow &&
                    a.StartTime > DateTime.UtcNow);

                var popular = popularActivities
                    .OrderByDescending(a => a.CurrentParticipants)
                    .Take(count);
                
                return _mapper.Map<IEnumerable<ActivityDto>>(popular);
            }

            // 找到有相似活动偏好的用户
            var allRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.Status == RegistrationStatus.Registered);
            
            if (!allRegistrations.Any())
            {
                _logger.LogInformation("系统中没有任何报名记录");
                return Enumerable.Empty<ActivityDto>();
            }

            var similarUsers = FindSimilarUsers(userId, userActivityIds, allRegistrations);
            
            if (!similarUsers.Any())
            {
                _logger.LogInformation("没有找到相似用户，用户ID: {UserId}", userId);
                // 返回热门但用户未报名的活动
                var unregisteredActivities = await _unitOfWork.Activities.FindAsync(a => 
                    !userActivityIds.Contains(a.Id) &&
                    a.Status == ActivityStatus.Published && 
                    a.RegistrationDeadline > DateTime.UtcNow &&
                    a.StartTime > DateTime.UtcNow);

                var result = unregisteredActivities
                    .OrderByDescending(a => a.CurrentParticipants)
                    .Take(count);
                
                return _mapper.Map<IEnumerable<ActivityDto>>(result);
            }

            // 基于相似用户推荐活动
            var recommendedActivityIds = new HashSet<int>();
            foreach (var similarUserId in similarUsers.Take(10))
            {
                var similarUserRegistrations = allRegistrations.Where(r => r.UserId == similarUserId.Key);
                foreach (var registration in similarUserRegistrations)
                {
                    if (!userActivityIds.Contains(registration.ActivityId))
                    {
                        recommendedActivityIds.Add(registration.ActivityId);
                    }
                }
            }

            if (!recommendedActivityIds.Any())
            {
                _logger.LogInformation("没有找到推荐活动，用户ID: {UserId}", userId);
                return Enumerable.Empty<ActivityDto>();
            }

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                recommendedActivityIds.Contains(a.Id) && 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow &&
                a.StartTime > DateTime.UtcNow);

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
            // 验证用户是否存在
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，用户ID: {UserId}", userId);
                return Enumerable.Empty<ActivityDto>();
            }

            // 基于内容的推荐
            var userPreferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
            var preferenceDict = userPreferences.ToDictionary(p => p.CategoryId, p => p.Weight);

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow &&
                a.StartTime > DateTime.UtcNow);

            if (!activities.Any())
            {
                _logger.LogInformation("没有可推荐的活动");
                return Enumerable.Empty<ActivityDto>();
            }

            // 获取用户已报名的活动
            var userRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.UserId == userId && r.Status == RegistrationStatus.Registered);
            var registeredActivityIds = userRegistrations.Select(r => r.ActivityId).ToHashSet();

            IEnumerable<Activity> scoredActivities;

            if (preferenceDict.Any())
            {
                // 如果有偏好设置，基于偏好推荐
                scoredActivities = activities
                    .Where(a => !registeredActivityIds.Contains(a.Id))
                    .Select(a => new
                    {
                        Activity = a,
                        Score = preferenceDict.ContainsKey(a.CategoryId) ? preferenceDict[a.CategoryId] : 0.1
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Activity.CurrentParticipants)
                    .Take(count)
                    .Select(x => x.Activity);
            }
            else
            {
                // 如果没有偏好设置，返回热门活动
                _logger.LogInformation("用户没有偏好设置，返回热门活动，用户ID: {UserId}", userId);
                scoredActivities = activities
                    .Where(a => !registeredActivityIds.Contains(a.Id))
                    .OrderByDescending(a => a.CurrentParticipants)
                    .ThenBy(a => Math.Abs((a.StartTime - DateTime.UtcNow).TotalDays))
                    .Take(count);
            }

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

        // 预热缓存
        _ = await GetRecommendedActivitiesAsync(userId);
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
            else
            {
                // 如果没有偏好设置，给予基础分数
                score += 0.1;
            }

            // 活动热度分数
            double popularityScore = CalculatePopularityScore(activity);
            score += popularityScore * 0.3;

            // 时间衰减因子
            double timeDecay = CalculateTimeDecayFactor(activity.StartTime);
            score += timeDecay * 0.3;

            // 报名比例分数（避免除零错误）
            if (activity.MaxParticipants > 0)
            {
                double fillRate = (double)activity.CurrentParticipants / activity.MaxParticipants;
                // 适中的报名比例更受欢迎（0.3-0.7之间）
                if (fillRate >= 0.3 && fillRate <= 0.7)
                {
                    score += 0.2;
                }
                else if (fillRate < 0.9) // 避免推荐快满的活动
                {
                    score += 0.1;
                }
            }
            else
            {
                score += 0.1; // 无限制活动给予基础分数
            }

            scoredActivities.Add((activity, score));
        }

        // 如果没有足够的活动满足最小分数要求，降低阈值
        var validActivities = scoredActivities.Where(x => x.score > 0.1);
        if (!validActivities.Any())
        {
            validActivities = scoredActivities; // 返回所有活动
        }

        var topActivities = validActivities
            .OrderByDescending(x => x.score)
            .Take(20) // 取前20个而不是使用常量
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