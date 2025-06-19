using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;
using System.Runtime.InteropServices;

// 注意：在实际项目中，当C++/CLI库成功编译后，取消注释下面这行
// using CampusActivity.NativeLib;

namespace CampusActivity.Application.Services;

/// <summary>
/// 增强的推荐服务，集成C++原生模块和C++/CLI混合编程
/// 展示了如何在.NET应用中调用原生C++代码
/// </summary>
public class EnhancedRecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<EnhancedRecommendationService> _logger;
    private readonly IDistributedCache _cache;

    // C++原生库函数声明（P/Invoke方式）
    [DllImport("CampusActivity.Core.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int AnalyzeActivityTrends(
        [In] int[] activityIds, 
        [In] double[] participationRates, 
        int count,
        [Out] double[] trendScores);

    [DllImport("CampusActivity.Core.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern double CalculateSimilarity(
        [In] int[] userActivities1,
        [In] int[] userActivities2,
        int count1,
        int count2);

    public EnhancedRecommendationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<EnhancedRecommendationService> logger,
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

            // 集成多种推荐算法
            var collaborativeRecommendations = await GetCollaborativeRecommendationsWithCppAsync(userId, count / 2);
            var contentBasedRecommendations = await GetContentBasedRecommendationsAsync(userId, count / 2);
            var hybridRecommendations = await GetHybridRecommendationsAsync(userId, count);

            // 合并和去重推荐结果
            var combinedRecommendations = collaborativeRecommendations
                .Concat(contentBasedRecommendations)
                .Concat(hybridRecommendations)
                .GroupBy(a => a.Id)
                .Select(g => g.First())
                .Take(count)
                .ToList();

            // 缓存结果
            var serializedRecommendations = JsonSerializer.Serialize(combinedRecommendations);
            await _cache.SetStringAsync(cacheKey, serializedRecommendations, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Medium
            });

            return combinedRecommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推荐活动失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<ActivityDto>();
        }
    }

    public async Task<IEnumerable<ActivityDto>> GetCollaborativeRecommendationsAsync(int userId, int count = 10)
    {
        return await GetCollaborativeRecommendationsWithCppAsync(userId, count);
    }

    /// <summary>
    /// 使用C++原生库进行协同过滤推荐
    /// </summary>
    private async Task<IEnumerable<ActivityDto>> GetCollaborativeRecommendationsWithCppAsync(int userId, int count = 10)
    {
        try
        {
            // 获取所有用户的活动参与数据
            var allRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
                r.Status == RegistrationStatus.Registered);
            
            var userActivities = allRegistrations.Where(r => r.UserId == userId)
                .Select(r => r.ActivityId).ToArray();

            var similarUsers = new List<(int UserId, double Similarity)>();

            // 使用C++原生库计算用户相似度
            var userGroups = allRegistrations.GroupBy(r => r.UserId)
                .Where(g => g.Key != userId)
                .ToList();

            foreach (var userGroup in userGroups)
            {
                var otherUserActivities = userGroup.Select(r => r.ActivityId).ToArray();
                
                try
                {
                    // 调用C++原生函数计算相似度
                    double similarity = CalculateSimilarity(
                        userActivities, 
                        otherUserActivities, 
                        userActivities.Length, 
                        otherUserActivities.Length);
                    
                    if (similarity > 0.1) // 相似度阈值
                    {
                        similarUsers.Add((userGroup.Key, similarity));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "C++相似度计算失败，用户ID: {UserId}", userGroup.Key);
                    // 降级到C#实现
                    var intersection = userActivities.Intersect(otherUserActivities).Count();
                    var union = userActivities.Union(otherUserActivities).Count();
                    if (union > 0)
                    {
                        double similarity = (double)intersection / union;
                        if (similarity > 0.1)
                        {
                            similarUsers.Add((userGroup.Key, similarity));
                        }
                    }
                }
            }

            // 基于相似用户推荐活动
            var recommendedActivityIds = new HashSet<int>();
            var topSimilarUsers = similarUsers.OrderByDescending(u => u.Similarity).Take(10);

            foreach (var (similarUserId, _) in topSimilarUsers)
            {
                var similarUserActivities = allRegistrations
                    .Where(r => r.UserId == similarUserId)
                    .Select(r => r.ActivityId)
                    .Where(id => !userActivities.Contains(id));

                foreach (var activityId in similarUserActivities)
                {
                    recommendedActivityIds.Add(activityId);
                }
            }

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                recommendedActivityIds.Contains(a.Id) && 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow);

            return _mapper.Map<IEnumerable<ActivityDto>>(activities.Take(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "C++协同过滤推荐失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<ActivityDto>();
        }
    }

    public async Task<IEnumerable<ActivityDto>> GetContentBasedRecommendationsAsync(int userId, int count = 10)
    {
        try
        {
            // 在实际项目中，这里可以调用C++/CLI库的内容推荐算法
            // var recommendations = RecommendationEngine.GetContentBasedRecommendations(userId, count);

            var userPreferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
            var preferenceDict = userPreferences.ToDictionary(p => p.CategoryId, p => p.Weight);

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow);

            var scoredActivities = activities.Select(a => new
            {
                Activity = a,
                Score = CalculateContentScore(a, preferenceDict)
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

    /// <summary>
    /// 混合推荐算法，结合C++数据分析
    /// </summary>
    private async Task<IEnumerable<ActivityDto>> GetHybridRecommendationsAsync(int userId, int count = 10)
    {
        try
        {
            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow);

            var activityArray = activities.ToArray();
            var activityIds = activityArray.Select(a => a.Id).ToArray();
            var participationRates = activityArray.Select(a => 
                (double)a.CurrentParticipants / a.MaxParticipants).ToArray();

            var trendScores = new double[activityArray.Length];

            try
            {
                // 调用C++原生库分析活动趋势
                int result = AnalyzeActivityTrends(
                    activityIds, 
                    participationRates, 
                    activityArray.Length, 
                    trendScores);

                if (result == 0) // 成功
                {
                    var hybridScores = new List<(Activity Activity, double Score)>();
                    
                    for (int i = 0; i < activityArray.Length; i++)
                    {
                        var activity = activityArray[i];
                        var trendScore = trendScores[i];
                        var timeDecay = CalculateTimeDecayFactor(activity.StartTime);
                        var hybridScore = trendScore * timeDecay;
                        
                        hybridScores.Add((activity, hybridScore));
                    }

                    var topActivities = hybridScores
                        .OrderByDescending(x => x.Score)
                        .Take(count)
                        .Select(x => x.Activity);

                    return _mapper.Map<IEnumerable<ActivityDto>>(topActivities);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "C++趋势分析失败，降级到C#实现");
            }

            // 降级到C#实现
            var fallbackScores = activityArray.Select(a => new
            {
                Activity = a,
                Score = CalculatePopularityScore(a) * CalculateTimeDecayFactor(a.StartTime)
            })
            .OrderByDescending(x => x.Score)
            .Take(count)
            .Select(x => x.Activity);

            return _mapper.Map<IEnumerable<ActivityDto>>(fallbackScores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "混合推荐算法失败，用户ID: {UserId}", userId);
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
        var cacheKey = string.Format(AppConstants.CacheKeys.RecommendedActivities, userId);
        await _cache.RemoveAsync(cacheKey);
        await GetRecommendedActivitiesAsync(userId);
    }

    #region 私有辅助方法

    private double CalculateContentScore(Activity activity, Dictionary<int, double> userPreferences)
    {
        double baseScore = userPreferences.ContainsKey(activity.CategoryId) 
            ? userPreferences[activity.CategoryId] : 0.1;

        // 考虑活动热度
        double popularityBonus = CalculatePopularityScore(activity) * 0.2;
        
        // 考虑时间因素
        double timeFactor = CalculateTimeDecayFactor(activity.StartTime);
        
        return (baseScore + popularityBonus) * timeFactor;
    }

    private double CalculatePopularityScore(Activity activity)
    {
        if (activity.MaxParticipants == 0) return 0;
        
        double registrationRatio = (double)activity.CurrentParticipants / activity.MaxParticipants;
        return Math.Min(1.0, registrationRatio * 1.2); // 轻微加权热门活动
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

    #endregion
} 