using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.Repositories;
using CampusActivity.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CampusActivity.Application.Services;

public class UserContextService : CampusActivity.Shared.DTOs.IUserContextService
{
    private readonly IRepository<ActivityRegistration> _registrationRepository;
    private readonly IRepository<ScheduleItem> _scheduleRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IScheduleService _scheduleService;

    public UserContextService(
        IRepository<ActivityRegistration> registrationRepository,
        IRepository<ScheduleItem> scheduleRepository,
        IRepository<User> userRepository,
        IScheduleService scheduleService)
    {
        _registrationRepository = registrationRepository;
        _scheduleRepository = scheduleRepository;
        _userRepository = userRepository;
        _scheduleService = scheduleService;
    }

    public async Task<UserContextDto> GetUserContextAsync(int userId)
    {
        var context = new UserContextDto();

        // 获取用户基本信息
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            context.User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Avatar = user.Avatar,
                Role = user.Role,
                StudentId = user.StudentId,
                Major = user.Major,
                Grade = user.Grade,
                EmployeeId = user.EmployeeId,
                Department = user.Department,
                Title = user.Title
            };
        }

        // 获取用户报名的活动
        var registrations = await _registrationRepository.FindAsync(r => r.UserId == userId);
        // 手动加载Activity（如果需要）
        foreach (var reg in registrations)
        {
            if (reg.Activity == null)
            {
                // 这里假设有DbContext可用，否则可扩展Repository
                // 或者只用ActivityId和Title等基本信息
                reg.Activity = new Activity { Title = "" };
            }
        }

        context.RegisteredActivities = registrations.Select(r => new ActivityRegistrationDto
        {
            Id = r.Id,
            ActivityId = r.ActivityId,
            ActivityTitle = r.Activity?.Title ?? "",
            UserId = r.UserId,
            UserName = user?.FullName ?? "",
            RegistrationTime = r.RegistrationTime,
            Status = r.Status,
            Note = r.Note
        }).ToList();

        // 获取即将到来的日程
        context.UpcomingScheduleItems = (await _scheduleService.GetUpcomingItemsAsync(userId, 10)).ToList();

        // 获取过期的日程
        context.OverdueScheduleItems = (await _scheduleService.GetOverdueItemsAsync(userId)).ToList();

        // 获取日程统计
        context.ScheduleStatistics = await _scheduleService.GetScheduleStatisticsAsync(userId);

        return context;
    }

    public async Task<string> GetUserContextSummaryAsync(int userId)
    {
        var context = await GetUserContextAsync(userId);
        
        var summary = $"用户信息：{context.User.FullName}（{context.User.Username}）";
        
        if (!string.IsNullOrEmpty(context.User.Major))
        {
            summary += $"，专业：{context.User.Major}";
        }
        
        if (context.User.Grade.HasValue)
        {
            summary += $"，年级：{context.User.Grade}";
        }

        // 添加报名活动信息
        if (context.RegisteredActivities.Any())
        {
            summary += $"\n\n已报名活动（{context.RegisteredActivities.Count}个）：";
            foreach (var activity in context.RegisteredActivities.Take(5))
            {
                summary += $"\n- {activity.ActivityTitle}（报名时间：{activity.RegistrationTime:MM-dd HH:mm}，状态：{activity.Status}）";
            }
            if (context.RegisteredActivities.Count > 5)
            {
                summary += $"\n... 还有{context.RegisteredActivities.Count - 5}个活动";
            }
        }

        // 添加即将到来的日程
        if (context.UpcomingScheduleItems.Any())
        {
            summary += $"\n\n即将到来的日程（{context.UpcomingScheduleItems.Count}个）：";
            foreach (var item in context.UpcomingScheduleItems.Take(5))
            {
                summary += $"\n- {item.Title}（{item.StartTime:MM-dd HH:mm}，{item.Location ?? "未指定地点"}）";
            }
            if (context.UpcomingScheduleItems.Count > 5)
            {
                summary += $"\n... 还有{context.UpcomingScheduleItems.Count - 5}个日程";
            }
        }

        // 添加过期日程
        if (context.OverdueScheduleItems.Any())
        {
            summary += $"\n\n过期未完成的日程（{context.OverdueScheduleItems.Count}个）：";
            foreach (var item in context.OverdueScheduleItems.Take(3))
            {
                summary += $"\n- {item.Title}（应于{item.StartTime:MM-dd HH:mm}完成）";
            }
        }

        // 添加统计信息
        summary += $"\n\n日程统计：";
        summary += $"\n- 总日程数：{context.ScheduleStatistics.TotalItems}";
        summary += $"\n- 已完成：{context.ScheduleStatistics.CompletedItems}";
        summary += $"\n- 未完成：{context.ScheduleStatistics.PendingItems}";
        summary += $"\n- 完成率：{context.ScheduleStatistics.CompletionRate:P1}";

        return summary;
    }
} 