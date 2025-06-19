using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;

namespace CampusActivity.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ActivityService> _logger;
    private readonly IDistributedCache _cache;

    public ActivityService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ActivityService> logger,
        IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<PagedResultDto<ActivityDto>> GetActivitiesAsync(ActivitySearchDto searchDto)
    {
        var query = await _unitOfWork.Activities.FindAsync(a => true);
        var activities = query.AsQueryable();

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

        if (searchDto.Status.HasValue)
        {
            activities = activities.Where(a => a.Status == searchDto.Status.Value);
        }

        if (searchDto.StartDate.HasValue)
        {
            activities = activities.Where(a => a.StartTime >= searchDto.StartDate.Value);
        }

        if (searchDto.EndDate.HasValue)
        {
            activities = activities.Where(a => a.EndTime <= searchDto.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Location))
        {
            activities = activities.Where(a => a.Location.Contains(searchDto.Location));
        }

        if (searchDto.IsRegisterable.HasValue && searchDto.IsRegisterable.Value)
        {
            activities = activities.Where(a => 
                a.Status == ActivityStatus.Published && 
                a.RegistrationDeadline > DateTime.UtcNow &&
                a.CurrentParticipants < a.MaxParticipants);
        }

        // 总数量
        var totalCount = activities.Count();

        // 排序
        switch (searchDto.SortBy.ToLower())
        {
            case "starttime":
                activities = searchDto.SortDescending 
                    ? activities.OrderByDescending(a => a.StartTime)
                    : activities.OrderBy(a => a.StartTime);
                break;
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
            default:
                activities = activities.OrderBy(a => a.StartTime);
                break;
        }

        // 分页
        var pagedActivities = activities
            .Skip((searchDto.PageIndex - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        var activityDtos = _mapper.Map<List<ActivityDto>>(pagedActivities);

        return new PagedResultDto<ActivityDto>
        {
            Items = activityDtos,
            TotalCount = totalCount,
            PageIndex = searchDto.PageIndex,
            PageSize = searchDto.PageSize
        };
    }

    public async Task<ActivityDto?> GetActivityByIdAsync(int id, int? currentUserId = null)
    {
        var activity = await _unitOfWork.Activities.GetByIdAsync(id);
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

    public async Task<IEnumerable<ActivityDto>> GetUserRegisteredActivitiesAsync(int userId)
    {
        var registrations = await _unitOfWork.ActivityRegistrations.FindAsync(r => 
            r.UserId == userId && r.Status == RegistrationStatus.Registered);
        
        var activityIds = registrations.Select(r => r.ActivityId).ToList();
        var activities = await _unitOfWork.Activities.FindAsync(a => activityIds.Contains(a.Id));
        
        return _mapper.Map<IEnumerable<ActivityDto>>(activities);
    }

    public async Task<IEnumerable<ActivityCategoryDto>> GetCategoriesAsync()
    {
        var cacheKey = AppConstants.CacheKeys.Categories;
        var cachedCategories = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedCategories))
        {
            return JsonSerializer.Deserialize<IEnumerable<ActivityCategoryDto>>(cachedCategories)!;
        }

        var categories = await _unitOfWork.ActivityCategories.FindAsync(c => c.IsActive);
        var categoryDtos = _mapper.Map<IEnumerable<ActivityCategoryDto>>(categories);

        var serializedCategories = JsonSerializer.Serialize(categoryDtos);
        await _cache.SetStringAsync(cacheKey, serializedCategories, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Long
        });

        return categoryDtos;
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
        var cacheKey = AppConstants.CacheKeys.PopularActivities;
        var cachedActivities = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedActivities))
        {
            return JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(cachedActivities)!;
        }

        var activities = await _unitOfWork.Activities.FindAsync(a => 
            a.Status == ActivityStatus.Published);
        
        var popularActivities = activities
            .OrderByDescending(a => a.CurrentParticipants)
            .ThenByDescending(a => a.CreatedAt)
            .Take(count)
            .ToList();

        var activityDtos = _mapper.Map<IEnumerable<ActivityDto>>(popularActivities);

        var serializedActivities = JsonSerializer.Serialize(activityDtos);
        await _cache.SetStringAsync(cacheKey, serializedActivities, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AppConstants.CacheExpiration.Medium
        });

        return activityDtos;
    }
} 