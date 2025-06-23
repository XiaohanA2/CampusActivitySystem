using AutoMapper;
using Microsoft.Extensions.Logging;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Enums;

namespace CampusActivity.Application.Services;

public class ScheduleService : IScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ScheduleService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ScheduleItemDto> CreateScheduleItemAsync(CreateScheduleItemDto createDto, int userId)
    {
        try
        {
            var scheduleItem = _mapper.Map<ScheduleItem>(createDto);
            scheduleItem.UserId = userId;

            await _unitOfWork.ScheduleItems.AddAsync(scheduleItem);
            await _unitOfWork.SaveChangesAsync();

            var result = _mapper.Map<ScheduleItemDto>(scheduleItem);
            
            // 如果是活动相关的日程，加载活动信息
            if (scheduleItem.ActivityId.HasValue)
            {
                var activity = await _unitOfWork.Activities.GetByIdAsync(scheduleItem.ActivityId.Value);
                if (activity != null)
                {
                    result.ActivityTitle = activity.Title;
                    result.ActivityLocation = activity.Location;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建日程项失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<ScheduleItemDto?> GetScheduleItemByIdAsync(int id, int userId)
    {
        var scheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s => 
            s.Id == id && s.UserId == userId);

        if (scheduleItem == null) return null;

        var result = _mapper.Map<ScheduleItemDto>(scheduleItem);
        
        // 加载活动信息
        if (scheduleItem.ActivityId.HasValue)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(scheduleItem.ActivityId.Value);
            if (activity != null)
            {
                result.ActivityTitle = activity.Title;
                result.ActivityLocation = activity.Location;
            }
        }

        return result;
    }

    public async Task<PagedResultDto<ScheduleItemDto>> GetUserScheduleItemsAsync(ScheduleSearchDto searchDto, int userId)
    {
        // 构建查询条件
        var items = await _unitOfWork.ScheduleItems.FindAsync(s => s.UserId == userId);
        var query = items.AsQueryable();

        // 应用筛选条件
        if (searchDto.StartDate.HasValue)
        {
            query = query.Where(s => s.StartTime >= searchDto.StartDate.Value);
        }

        if (searchDto.EndDate.HasValue)
        {
            query = query.Where(s => s.EndTime <= searchDto.EndDate.Value);
        }

        if (searchDto.Type.HasValue)
        {
            query = query.Where(s => s.Type == searchDto.Type.Value);
        }

        if (searchDto.Priority.HasValue)
        {
            query = query.Where(s => s.Priority == searchDto.Priority.Value);
        }

        if (searchDto.IsCompleted.HasValue)
        {
            query = query.Where(s => s.IsCompleted == searchDto.IsCompleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
        {
            query = query.Where(s => s.Title.Contains(searchDto.Keyword) || 
                                   (s.Description != null && s.Description.Contains(searchDto.Keyword)));
        }

        // 总数量
        var totalCount = query.Count();

        // 排序和分页
        var pagedItems = query
            .OrderBy(s => s.StartTime)
            .Skip((searchDto.PageIndex - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        var scheduleItemDtos = _mapper.Map<List<ScheduleItemDto>>(pagedItems);

        // 加载活动信息
        foreach (var dto in scheduleItemDtos.Where(s => s.ActivityId.HasValue))
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId.Value);
            if (activity != null)
            {
                dto.ActivityTitle = activity.Title;
                dto.ActivityLocation = activity.Location;
            }
        }

        return new PagedResultDto<ScheduleItemDto>
        {
            Items = scheduleItemDtos,
            TotalCount = totalCount,
            PageIndex = searchDto.PageIndex,
            PageSize = searchDto.PageSize
        };
    }

    public async Task<ScheduleItemDto> UpdateScheduleItemAsync(int id, UpdateScheduleItemDto updateDto, int userId)
    {
        var scheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s => 
            s.Id == id && s.UserId == userId);

        if (scheduleItem == null)
        {
            throw new KeyNotFoundException("日程项不存在");
        }

        // 更新字段
        scheduleItem.Title = updateDto.Title;
        scheduleItem.Description = updateDto.Description;
        scheduleItem.Location = updateDto.Location;
        scheduleItem.StartTime = updateDto.StartTime;
        scheduleItem.EndTime = updateDto.EndTime;
        scheduleItem.Type = updateDto.Type;
        scheduleItem.Priority = updateDto.Priority;
        scheduleItem.Color = updateDto.Color;
        scheduleItem.Note = updateDto.Note;
        scheduleItem.IsCompleted = updateDto.IsCompleted;
        scheduleItem.ActivityId = updateDto.ActivityId;

        await _unitOfWork.ScheduleItems.UpdateAsync(scheduleItem);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<ScheduleItemDto>(scheduleItem);
        
        // 加载活动信息
        if (scheduleItem.ActivityId.HasValue)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(scheduleItem.ActivityId.Value);
            if (activity != null)
            {
                result.ActivityTitle = activity.Title;
                result.ActivityLocation = activity.Location;
            }
        }

        return result;
    }

    public async Task<bool> DeleteScheduleItemAsync(int id, int userId)
    {
        var scheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s => 
            s.Id == id && s.UserId == userId);

        if (scheduleItem == null) return false;

        await _unitOfWork.ScheduleItems.DeleteAsync(scheduleItem);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleScheduleItemCompletionAsync(int id, int userId)
    {
        var scheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s => 
            s.Id == id && s.UserId == userId);

        if (scheduleItem == null) return false;

        scheduleItem.IsCompleted = !scheduleItem.IsCompleted;
        await _unitOfWork.ScheduleItems.UpdateAsync(scheduleItem);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<ScheduleCalendarDto>> GetCalendarViewAsync(DateTime startDate, DateTime endDate, int userId)
    {
        var scheduleItems = await _unitOfWork.ScheduleItems.FindAsync(s => 
            s.UserId == userId && 
            s.StartTime >= startDate && 
            s.StartTime <= endDate);

        var calendarData = new List<ScheduleCalendarDto>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var dayItems = scheduleItems.Where(s => s.StartTime.Date == currentDate).ToList();
            var dayDto = new ScheduleCalendarDto
            {
                Date = currentDate,
                Items = _mapper.Map<List<ScheduleItemDto>>(dayItems),
                TotalCount = dayItems.Count(),
                CompletedCount = dayItems.Count(s => s.IsCompleted),
                PendingCount = dayItems.Count(s => !s.IsCompleted)
            };

            // 加载活动信息
            foreach (var item in dayDto.Items.Where(s => s.ActivityId.HasValue))
            {
                var activity = await _unitOfWork.Activities.GetByIdAsync(item.ActivityId.Value);
                if (activity != null)
                {
                    item.ActivityTitle = activity.Title;
                    item.ActivityLocation = activity.Location;
                }
            }

            calendarData.Add(dayDto);
            currentDate = currentDate.AddDays(1);
        }

        return calendarData;
    }

    public async Task<ScheduleStatisticsDto> GetScheduleStatisticsAsync(int userId)
    {
        var allItems = await _unitOfWork.ScheduleItems.FindAsync(s => s.UserId == userId);
        var now = DateTime.UtcNow;

        var statistics = new ScheduleStatisticsDto
        {
            TotalItems = allItems.Count(),
            CompletedItems = allItems.Count(s => s.IsCompleted),
            PendingItems = allItems.Count(s => !s.IsCompleted && s.EndTime > now),
            OverdueItems = allItems.Count(s => !s.IsCompleted && s.EndTime <= now),
            ItemsByType = allItems.GroupBy(s => s.Type)
                                 .ToDictionary(g => g.Key, g => g.Count()),
            ItemsByPriority = allItems.GroupBy(s => s.Priority)
                                     .ToDictionary(g => g.Key, g => g.Count())
        };

        statistics.CompletionRate = statistics.TotalItems > 0 
            ? (double)statistics.CompletedItems / statistics.TotalItems * 100 
            : 0;

        return statistics;
    }

    public async Task<bool> AddActivityToScheduleAsync(int activityId, int userId)
    {
        try
        {
            // 检查是否已存在
            var existingItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s => 
                s.ActivityId == activityId && s.UserId == userId);

            if (existingItem != null)
            {
                return true; // 已存在，不需要重复添加
            }

            // 获取活动信息
            var activity = await _unitOfWork.Activities.GetByIdAsync(activityId);
            if (activity == null)
            {
                throw new KeyNotFoundException("活动不存在");
            }

            // 检查用户是否已报名该活动
            var registration = await _unitOfWork.ActivityRegistrations.FirstOrDefaultAsync(r => 
                r.ActivityId == activityId && r.UserId == userId && 
                r.Status == RegistrationStatus.Registered);

            if (registration == null)
            {
                throw new InvalidOperationException("您尚未报名此活动");
            }

            // 创建日程项
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
                ActivityId = activityId,
                IsCompleted = false
            };

            await _unitOfWork.ScheduleItems.AddAsync(scheduleItem);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加活动到日程表失败，活动ID: {ActivityId}, 用户ID: {UserId}", activityId, userId);
            throw;
        }
    }

    public async Task<bool> RemoveActivityFromScheduleAsync(int activityId, int userId)
    {
        var scheduleItem = await _unitOfWork.ScheduleItems.FirstOrDefaultAsync(s => 
            s.ActivityId == activityId && s.UserId == userId);

        if (scheduleItem == null) return false;

        await _unitOfWork.ScheduleItems.DeleteAsync(scheduleItem);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<ScheduleItemDto>> GetUpcomingItemsAsync(int userId, int count = 10)
    {
        var now = DateTime.UtcNow;
        var upcomingItems = await _unitOfWork.ScheduleItems.FindAsync(s => 
            s.UserId == userId && 
            s.StartTime > now && 
            !s.IsCompleted);

        var result = upcomingItems
            .OrderBy(s => s.StartTime)
            .Take(count)
            .ToList();

        var scheduleItemDtos = _mapper.Map<List<ScheduleItemDto>>(result);

        // 加载活动信息
        foreach (var dto in scheduleItemDtos.Where(s => s.ActivityId.HasValue))
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId.Value);
            if (activity != null)
            {
                dto.ActivityTitle = activity.Title;
                dto.ActivityLocation = activity.Location;
            }
        }

        return scheduleItemDtos;
    }

    public async Task<IEnumerable<ScheduleItemDto>> GetOverdueItemsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var overdueItems = await _unitOfWork.ScheduleItems.FindAsync(s => 
            s.UserId == userId && 
            s.EndTime <= now && 
            !s.IsCompleted);

        var result = overdueItems.OrderBy(s => s.StartTime).ToList();
        var scheduleItemDtos = _mapper.Map<List<ScheduleItemDto>>(result);

        // 加载活动信息
        foreach (var dto in scheduleItemDtos.Where(s => s.ActivityId.HasValue))
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId.Value);
            if (activity != null)
            {
                dto.ActivityTitle = activity.Title;
                dto.ActivityLocation = activity.Location;
            }
        }

        return scheduleItemDtos;
    }
} 