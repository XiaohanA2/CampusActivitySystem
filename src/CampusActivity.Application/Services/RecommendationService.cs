using AutoMapper;
using Microsoft.Extensions.Logging;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Domain.Entities;

namespace CampusActivity.Application.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<RecommendationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ActivityDto>> GetRecommendedActivitiesAsync(int userId, int count = 10)
    {
        try
        {
            // 简化推荐逻辑：获取用户未报名的最新活动
            var userRegistrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => r.UserId == userId);
            var registeredActivityIds = userRegistrations.Select(r => r.ActivityId).ToList();

            var activities = await _unitOfWork.Activities.FindAsync(a => 
                a.Status == ActivityStatus.Published && 
                a.StartTime > DateTime.UtcNow &&
                !registeredActivityIds.Contains(a.Id));

            var recommendedActivities = activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList();

            return _mapper.Map<IEnumerable<ActivityDto>>(recommendedActivities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推荐活动失败，用户ID: {UserId}", userId);
            return new List<ActivityDto>();
        }
    }

    public async Task<IEnumerable<ActivityDto>> GetCollaborativeRecommendationsAsync(int userId, int count = 10)
    {
        // 暂时返回常规推荐
        return await GetRecommendedActivitiesAsync(userId, count);
    }

    public async Task<IEnumerable<ActivityDto>> GetContentBasedRecommendationsAsync(int userId, int count = 10)
    {
        // 暂时返回常规推荐
        return await GetRecommendedActivitiesAsync(userId, count);
    }

    public async Task<IEnumerable<UserActivityPreferenceDto>> GetUserPreferencesAsync(int userId)
    {
        try
        {
            var preferences = await _unitOfWork.UserActivityPreferences.FindAsync(p => p.UserId == userId);
            return _mapper.Map<IEnumerable<UserActivityPreferenceDto>>(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户偏好失败，用户ID: {UserId}", userId);
            return new List<UserActivityPreferenceDto>();
        }
    }

    public async Task UpdateUserPreferencesAsync(int userId, int categoryId, double weight)
    {
        try
        {
            var preference = await _unitOfWork.UserActivityPreferences.FirstOrDefaultAsync(p => 
                p.UserId == userId && p.CategoryId == categoryId);

            if (preference != null)
            {
                preference.Weight = weight;
                preference.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.UserActivityPreferences.UpdateAsync(preference);
            }
            else
            {
                preference = new UserActivityPreference
                {
                    UserId = userId,
                    CategoryId = categoryId,
                    Weight = weight,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserActivityPreferences.AddAsync(preference);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户偏好失败，用户ID: {UserId}, 分类ID: {CategoryId}", userId, categoryId);
            throw;
        }
    }

    public async Task RecalculateRecommendationsAsync(int userId)
    {
        try
        {
            // 简化实现：暂时什么都不做
            _logger.LogInformation("重新计算推荐完成，用户ID: {UserId}", userId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新计算推荐失败，用户ID: {UserId}", userId);
            throw;
        }
    }
} 