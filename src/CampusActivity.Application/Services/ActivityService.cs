using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;
using CampusActivity.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CampusActivity.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ActivityService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ActivityService> logger,
        IDistributedCache cache,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    // 获取当前用户ID的辅助方法
    private int? GetCurrentUserIdFromContext()
    {
        try
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return null;
                
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResultDto<ActivityDto>> GetActivitiesAsync(ActivitySearchDto searchDto)
    {
        IQueryable<Activity> activities = _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Category)
            .Include(a => a.Creator)
            .Include(a => a.Tags);

        // 应用搜索过滤器
        if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
        {
            activities = activities.Where(a => 
                a.Title.Contains(searchDto.Keyword) || 
                a.Description.Contains(searchDto.Keyword));
        }

        if (searchDto.CategoryId.HasValue)
        {
            activities = activities.Where(a => a.CategoryId == searchDto.CategoryId.Value);
        }

        // 移除了Status过滤，该字段已从DTO中删除

        if (searchDto.StartDate.HasValue)
        {
            activities = activities.Where(a => a.StartTime >= searchDto.StartDate.Value);
        }

        if (searchDto.EndDate.HasValue)
        {
            activities = activities.Where(a => a.EndTime <= searchDto.EndDate.Value);
        }

        // 移除了Location过滤，该字段已从DTO中删除

        // 状态过滤
        if (searchDto.IsRegisterable.HasValue && searchDto.IsRegisterable.Value)
        {
            activities = activities.Where(a => 
                a.Status == ActivityStatus.Published &&
                a.RegistrationDeadline > DateTime.UtcNow &&
                a.CurrentParticipants < a.MaxParticipants);
        }

        // 对于复杂排序（热度、推荐），先获取所有数据到内存；对于简单排序，在数据库层面进行
        List<Activity> allActivities;
        int totalCount;
        
        if (searchDto.SortBy.ToLower() == "popularity" || searchDto.SortBy.ToLower() == "recommended")
        {
            // 复杂排序：先获取所有匹配的数据到内存
            allActivities = await activities.ToListAsync();
            totalCount = allActivities.Count;
            
            // 在内存中进行复杂排序
            switch (searchDto.SortBy.ToLower())
            {
                case "popularity":
                    // 按热度排序：综合考虑参与人数、报名率、活动新旧程度
                    allActivities = allActivities.OrderByDescending(a => 
                        (a.CurrentParticipants * 1.0 / Math.Max(a.MaxParticipants, 1)) * 0.4 + // 报名率权重40%
                        a.CurrentParticipants * 0.3 + // 参与人数权重30%
                        (1.0 / Math.Max((DateTime.UtcNow - a.CreatedAt).TotalDays + 1, 1)) * 0.3 // 新鲜度权重30%
                    ).ToList();
                    break;
                    
                case "recommended":
                    // 推荐排序：需要用户ID，如果没有则按热度排序
                    var currentUserId = GetCurrentUserIdFromContext();
                    if (currentUserId.HasValue)
                    {
                        // 基于用户偏好的推荐排序
                        var userPreferences = await _unitOfWork.UserActivityPreferences
                            .FindAsync(p => p.UserId == currentUserId.Value);
                        
                        if (userPreferences.Any())
                        {
                            var preferenceWeights = userPreferences.ToDictionary(p => p.CategoryId, p => p.Weight);
                            allActivities = allActivities.OrderByDescending(a => 
                                preferenceWeights.GetValueOrDefault(a.CategoryId, 0.5) * 0.6 + // 用户偏好权重60%
                                (a.CurrentParticipants * 1.0 / Math.Max(a.MaxParticipants, 1)) * 0.4 // 热度权重40%
                            ).ToList();
                        }
                        else
                        {
                            // 用户没有偏好，按热度排序
                            allActivities = allActivities.OrderByDescending(a => 
                                (a.CurrentParticipants * 1.0 / Math.Max(a.MaxParticipants, 1)) * 0.4 +
                                a.CurrentParticipants * 0.3 +
                                (1.0 / Math.Max((DateTime.UtcNow - a.CreatedAt).TotalDays + 1, 1)) * 0.3
                            ).ToList();
                        }
                    }
                    else
                    {
                        // 未登录用户，按热度排序
                        allActivities = allActivities.OrderByDescending(a => 
                            (a.CurrentParticipants * 1.0 / Math.Max(a.MaxParticipants, 1)) * 0.4 +
                            a.CurrentParticipants * 0.3 +
                            (1.0 / Math.Max((DateTime.UtcNow - a.CreatedAt).TotalDays + 1, 1)) * 0.3
                        ).ToList();
                    }
                    break;
            }
            
            // 内存分页
            var pagedMemoryActivities = allActivities
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToList();
                
            var activityDtos = _mapper.Map<List<ActivityDto>>(pagedMemoryActivities);
            
            return new PagedResultDto<ActivityDto>
            {
                Items = activityDtos,
                TotalCount = totalCount,
                PageIndex = searchDto.Page,
                PageSize = searchDto.PageSize
            };
        }
        else
        {
            // 简单排序：在数据库层面进行
            switch (searchDto.SortBy.ToLower())
            {
                case "createdat":
                    activities = searchDto.SortDescending 
                        ? activities.OrderByDescending(a => a.CreatedAt)
                        : activities.OrderBy(a => a.CreatedAt);
                    break;
                case "title":
                    activities = searchDto.SortDescending 
                        ? activities.OrderByDescending(a => a.Title)
                        : activities.OrderBy(a => a.Title);
                    break;
                case "currentparticipants":
                    activities = searchDto.SortDescending 
                        ? activities.OrderByDescending(a => a.CurrentParticipants)
                        : activities.OrderBy(a => a.CurrentParticipants);
                    break;
                case "starttime":
                default:
                    activities = searchDto.SortDescending 
                        ? activities.OrderByDescending(a => a.StartTime)
                        : activities.OrderBy(a => a.StartTime);
                    break;
            }

            // 数据库分页
            totalCount = await activities.CountAsync();
            var pagedActivities = await activities
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var activityDtos = _mapper.Map<List<ActivityDto>>(pagedActivities);

            return new PagedResultDto<ActivityDto>
            {
                Items = activityDtos,
                TotalCount = totalCount,
                PageIndex = searchDto.Page,
                PageSize = searchDto.PageSize
            };
        }
    }

    public async Task<ActivityDto?> GetActivityByIdAsync(int id, int? currentUserId = null)
    {
        var activity = await _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Category)
            .Include(a => a.Creator)
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (activity == null) return null;

        var activityDto = _mapper.Map<ActivityDto>(activity);

        // 检查当前用户是否已报名
        if (currentUserId.HasValue)
        {
            var registration = await _unitOfWork.ActivityRegistrations.FirstOrDefaultAsync(r =>
                r.ActivityId == id && r.UserId == currentUserId.Value && 
                r.Status == RegistrationStatus.Registered);
            activityDto.IsRegistered = registration != null;
        }

        return activityDto;
    }

    public async Task<ActivityDto> CreateActivityAsync(CreateActivityDto createDto, int createdBy)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var activity = _mapper.Map<Activity>(createDto);
            activity.CreatedBy = createdBy;
            activity.Status = ActivityStatus.Published;
            activity.CurrentParticipants = 0;

            await _unitOfWork.Activities.AddAsync(activity);
            await _unitOfWork.SaveChangesAsync();

            // 处理标签
            if (createDto.Tags.Any())
            {
                var tags = createDto.Tags.Select(tagName => new ActivityTag
                {
                    ActivityId = activity.Id,
                    TagName = tagName
                }).ToList();

                await _unitOfWork.ActivityTags.AddRangeAsync(tags);
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();

            // 清除缓存
            await _cache.RemoveAsync(AppConstants.CacheKeys.Activities);

            return _mapper.Map<ActivityDto>(activity);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ActivityDto> UpdateActivityAsync(int id, UpdateActivityDto updateDto, int updatedBy)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var activity = await _unitOfWork.Activities.GetByIdAsync(id);
            if (activity == null)
            {
                throw new KeyNotFoundException("活动不存在");
            }

            _mapper.Map(updateDto, activity);
            activity.UpdatedBy = updatedBy;

            await _unitOfWork.Activities.UpdateAsync(activity);

            // 更新标签
            var existingTags = await _unitOfWork.ActivityTags.FindAsync(t => t.ActivityId == id);
            await _unitOfWork.ActivityTags.DeleteRangeAsync(existingTags);

            if (updateDto.Tags.Any())
            {
                var newTags = updateDto.Tags.Select(tagName => new ActivityTag
                {
                    ActivityId = activity.Id,
                    TagName = tagName
                }).ToList();

                await _unitOfWork.ActivityTags.AddRangeAsync(newTags);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // 清除缓存
            await _cache.RemoveAsync(AppConstants.CacheKeys.Activities);

            return _mapper.Map<ActivityDto>(activity);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> DeleteActivityAsync(int id)
    {
        var activity = await _unitOfWork.Activities.GetByIdAsync(id);
        if (activity == null) return false;

        await _unitOfWork.Activities.DeleteAsync(activity);
        await _unitOfWork.SaveChangesAsync();

        // 清除缓存
        await _cache.RemoveAsync(AppConstants.CacheKeys.Activities);

        return true;
    }

    public async Task<bool> RegisterForActivityAsync(int activityId, int userId, string? note = null)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var activity = await _unitOfWork.Activities.GetByIdAsync(activityId);
            if (activity == null)
            {
                throw new KeyNotFoundException("活动不存在");
            }

            // 检查活动状态
            if (activity.Status != ActivityStatus.Published)
            {
                throw new InvalidOperationException("活动未发布");
            }

            // 检查报名截止时间
            if (activity.RegistrationDeadline < DateTime.UtcNow)
            {
                throw new InvalidOperationException("报名已截止");
            }

            // 检查人数限制
            if (activity.CurrentParticipants >= activity.MaxParticipants)
            {
                throw new InvalidOperationException("活动已满员");
            }

            // 检查是否已报名
            var existingRegistration = await _unitOfWork.ActivityRegistrations.FirstOrDefaultAsync(r =>
                r.ActivityId == activityId && r.UserId == userId);

            if (existingRegistration != null)
            {
                throw new InvalidOperationException("您已报名此活动");
            }

            // 创建报名记录
            var registration = new ActivityRegistration
            {
                ActivityId = activityId,
                UserId = userId,
                Note = note,
                Status = RegistrationStatus.Registered,
                RegistrationTime = DateTime.UtcNow
            };

            await _unitOfWork.ActivityRegistrations.AddAsync(registration);

            // 更新参与人数
            activity.CurrentParticipants++;
            await _unitOfWork.Activities.UpdateAsync(activity);

            // 自动将活动添加到用户的日程表
            var existingScheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.ActivityId == activityId);

            if (existingScheduleItem == null)
            {
                var scheduleItem = new ScheduleItem
                {
                    UserId = userId,
                    Title = $"参加活动：{activity.Title}",
                    Description = activity.Description,
                    Location = activity.Location,
                    StartTime = activity.StartTime,
                    EndTime = activity.EndTime,
                    Type = ScheduleItemType.Activity,
                    Priority = ScheduleItemPriority.Medium,
                    Color = "#007bff", // 蓝色表示活动
                    IsCompleted = false,
                    Note = note,
                    ActivityId = activityId
                };

                await _unitOfWork.ScheduleItems.AddAsync(scheduleItem);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> CancelRegistrationAsync(int activityId, int userId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var registration = await _unitOfWork.ActivityRegistrations.FirstOrDefaultAsync(r =>
                r.ActivityId == activityId && r.UserId == userId && 
                r.Status == RegistrationStatus.Registered);

            if (registration == null)
            {
                throw new KeyNotFoundException("报名记录不存在");
            }

            registration.Status = RegistrationStatus.Cancelled;
            await _unitOfWork.ActivityRegistrations.UpdateAsync(registration);

            // 更新参与人数
            var activity = await _unitOfWork.Activities.GetByIdAsync(activityId);
            if (activity != null)
            {
                activity.CurrentParticipants = Math.Max(0, activity.CurrentParticipants - 1);
                await _unitOfWork.Activities.UpdateAsync(activity);
            }

            // 从日程表中移除对应的活动
            var scheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.ActivityId == activityId);

            if (scheduleItem != null)
            {
                await _unitOfWork.ScheduleItems.DeleteAsync(scheduleItem);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<IEnumerable<ActivityRegistrationDto>> GetActivityRegistrationsAsync(int activityId)
    {
        var registrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => r.ActivityId == activityId);
        return _mapper.Map<IEnumerable<ActivityRegistrationDto>>(registrations);
    }

    public async Task<IEnumerable<ActivityCategoryDto>> GetCategoriesAsync()
    {
        try
        {
            var cacheKey = AppConstants.CacheKeys.Categories;
            string? cachedCategories = null;
            
            try
            {
                cachedCategories = await _cache.GetStringAsync(cacheKey);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Redis缓存读取失败，直接从数据库查询");
            }
            
            if (!string.IsNullOrEmpty(cachedCategories))
            {
                return JsonSerializer.Deserialize<IEnumerable<ActivityCategoryDto>>(cachedCategories)!;
            }

            var categories = await _unitOfWork.ActivityCategories.FindAsync(c => c.IsActive);
            var categoryDtos = _mapper.Map<IEnumerable<ActivityCategoryDto>>(categories);

            try
            {
                var serializedCategories = JsonSerializer.Serialize(categoryDtos);
                await _cache.SetStringAsync(cacheKey, serializedCategories, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Long
                });
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Redis缓存设置失败，继续使用数据库查询");
            }

            return categoryDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动分类失败");
            throw;
        }
    }

    public async Task<ActivityCategoryDto> CreateCategoryAsync(ActivityCategoryDto categoryDto)
    {
        var category = _mapper.Map<ActivityCategory>(categoryDto);
        await _unitOfWork.ActivityCategories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        // 清除缓存
        await _cache.RemoveAsync(AppConstants.CacheKeys.Categories);

        return _mapper.Map<ActivityCategoryDto>(category);
    }

    public async Task<bool> UpdateActivityStatusAsync(int activityId, ActivityStatus status)
    {
        var activity = await _unitOfWork.Activities.GetByIdAsync(activityId);
        if (activity == null) return false;

        activity.Status = status;
        await _unitOfWork.Activities.UpdateAsync(activity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<ActivityDto>> GetPopularActivitiesAsync(int count = 10)
    {
        try
        {
            var cacheKey = AppConstants.CacheKeys.PopularActivities;
            string? cachedActivities = null;
            
            try
            {
                cachedActivities = await _cache.GetStringAsync(cacheKey);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Redis缓存读取失败，直接从数据库查询");
            }
            
            if (!string.IsNullOrEmpty(cachedActivities))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(cachedActivities);
                    if (cached != null && cached.Any())
                    {
                        return cached;
                    }
                }
                catch (Exception deserializeEx)
                {
                    _logger.LogWarning(deserializeEx, "缓存数据反序列化失败，重新查询");
                }
            }

            // 查询已发布的活动（移除时间限制以便显示历史活动）
            var activities = await _unitOfWork.Activities.GetQueryable()
                .Include(a => a.Category)
                .Include(a => a.Creator)
                .Include(a => a.Tags)
                .Where(a => a.Status == ActivityStatus.Published)
                .ToListAsync();
            
            _logger.LogInformation($"查询到 {activities.Count} 个已发布的活动");
            
            if (!activities.Any())
            {
                _logger.LogInformation("没有找到符合条件的活动");
                return new List<ActivityDto>();
            }

            // 计算热门度：参与人数 + 最近创建加权
            var popularActivities = activities
                .Select(a => new
                {
                    Activity = a,
                    PopularityScore = a.CurrentParticipants * 2.0 + 
                                   (DateTime.UtcNow - a.CreatedAt).TotalDays < 7 ? 10 : 0 +
                                   (a.MaxParticipants > 0 ? (double)a.CurrentParticipants / a.MaxParticipants * 100 : 0)
                })
                .OrderByDescending(x => x.PopularityScore)
                .ThenByDescending(x => x.Activity.CreatedAt)
                .Take(count)
                .Select(x => x.Activity)
                .ToList();

            var activityDtos = _mapper.Map<IEnumerable<ActivityDto>>(popularActivities);

            try
            {
                var serializedActivities = JsonSerializer.Serialize(activityDtos, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await _cache.SetStringAsync(cacheKey, serializedActivities, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Medium
                });
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Redis缓存设置失败，继续使用数据库查询");
            }

            return activityDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门活动失败");
            // 不抛出异常，返回空列表以保证API稳定性
            return new List<ActivityDto>();
        }
    }
} 