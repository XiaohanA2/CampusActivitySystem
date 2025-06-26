using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CampusActivity.Infrastructure.Data;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;
using CampusActivity.Domain.Entities;
using AutoMapper;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppConstants.Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, IMapper mapper, ILogger<AdminController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    #region 用户管理

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // 搜索过滤
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) || 
                                       u.FullName.Contains(search) || 
                                       u.Email.Contains(search));
            }

            // 角色过滤
            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserDto>>(users);

            return Ok(new
            {
                Users = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return StatusCode(500, new { Message = "获取用户列表失败", Error = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户状态（激活/禁用）
    /// </summary>
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] bool isActive)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "用户不存在" });
            }

            // 防止禁用自己
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest(new { Message = "不能禁用自己的账户" });
            }

            user.IsActive = isActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"用户 {user.Username} 状态已更新为 {(isActive ? "激活" : "禁用")}");
            
            return Ok(new { Message = $"用户状态已更新为{(isActive ? "激活" : "禁用")}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新用户状态失败，用户ID: {id}");
            return StatusCode(500, new { Message = "更新用户状态失败", Error = ex.Message });
        }
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "用户不存在" });
            }

            // 重置为默认密码
            var defaultPassword = "123456";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"用户 {user.Username} 密码已重置");
            
            return Ok(new { Message = "密码已重置为默认密码: 123456" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"重置用户密码失败，用户ID: {id}");
            return StatusCode(500, new { Message = "重置密码失败", Error = ex.Message });
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "用户不存在" });
            }

            // 防止删除自己
            var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest(new { Message = "不能删除自己的账户" });
            }

            // 防止删除有关联数据的用户
            var hasActivities = await _context.Activities.AnyAsync(a => a.CreatedBy == id);
            var hasRegistrations = await _context.ActivityRegistrations.AnyAsync(r => r.UserId == id);
            
            if (hasActivities || hasRegistrations)
            {
                return BadRequest(new { Message = "该用户有关联的活动或报名记录，无法删除" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"用户 {user.Username} 已被删除");
            
            return Ok(new { Message = "用户已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除用户失败，用户ID: {id}");
            return StatusCode(500, new { Message = "删除用户失败", Error = ex.Message });
        }
    }

    #endregion

    #region 活动管理

    /// <summary>
    /// 获取所有活动（管理员视图）
    /// </summary>
    [HttpGet("activities")]
    public async Task<ActionResult> GetAllActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.Activities
                .Include(a => a.Category)
                .Include(a => a.Creator)
                .AsQueryable();

            // 搜索过滤
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Title.Contains(search) || a.Description.Contains(search));
            }

            // 分类过滤
            if (categoryId.HasValue)
            {
                query = query.Where(a => a.CategoryId == categoryId.Value);
            }

            // 状态过滤
            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.Now;
                query = status.ToLower() switch
                {
                    "upcoming" => query.Where(a => a.StartTime > now),
                    "ongoing" => query.Where(a => a.StartTime <= now && a.EndTime >= now),
                    "completed" => query.Where(a => a.EndTime < now),
                    "draft" => query.Where(a => a.Status == ActivityStatus.Draft),
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();
            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.StartTime,
                    a.EndTime,
                    a.Location,
                    a.MaxParticipants,
                    IsPublished = a.Status == ActivityStatus.Published,
                    a.CreatedAt,
                    Category = a.Category!.Name,
                    Creator = a.Creator!.FullName,
                    RegistrationCount = _context.ActivityRegistrations.Count(r => r.ActivityId == a.Id)
                })
                .ToListAsync();

            return Ok(new
            {
                Activities = activities,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动列表失败");
            return StatusCode(500, new { Message = "获取活动列表失败", Error = ex.Message });
        }
    }

    /// <summary>
    /// 强制删除活动
    /// </summary>
    [HttpDelete("activities/{id}")]
    public async Task<IActionResult> ForceDeleteActivity(int id)
    {
        try
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound(new { Message = "活动不存在" });
            }

            // 删除相关的报名记录
            var registrations = _context.ActivityRegistrations.Where(r => r.ActivityId == id);
            _context.ActivityRegistrations.RemoveRange(registrations);

            // 删除相关的推荐记录
            var recommendations = _context.ActivityRecommendations.Where(r => r.ActivityId == id);
            _context.ActivityRecommendations.RemoveRange(recommendations);

            // 删除相关的日程项
            var scheduleItems = _context.ScheduleItems.Where(s => s.ActivityId == id);
            _context.ScheduleItems.RemoveRange(scheduleItems);

            // 删除活动本身
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"活动 {activity.Title} 已被强制删除");
            
            return Ok(new { Message = "活动已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除活动失败，活动ID: {id}");
            return StatusCode(500, new { Message = "删除活动失败", Error = ex.Message });
        }
    }

    #endregion

    #region 系统统计

    /// <summary>
    /// 获取系统统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetSystemStatistics()
    {
        try
        {
            var now = DateTime.Now;
            var thirtyDaysAgo = now.AddDays(-30);

            // 基础统计
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var totalActivities = await _context.Activities.CountAsync();
            var publishedActivities = await _context.Activities.CountAsync(a => a.Status == ActivityStatus.Published);
            var totalRegistrations = await _context.ActivityRegistrations.CountAsync();

            // 最近30天统计
            var newUsersLast30Days = await _context.Users
                .CountAsync(u => u.CreatedAt >= thirtyDaysAgo);
            var newActivitiesLast30Days = await _context.Activities
                .CountAsync(a => a.CreatedAt >= thirtyDaysAgo);
            var newRegistrationsLast30Days = await _context.ActivityRegistrations
                .CountAsync(r => r.CreatedAt >= thirtyDaysAgo);

            // 用户角色分布
            var userRoleDistribution = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // 热门活动（报名人数最多的前10个）
            var popularActivities = await _context.Activities
                .Where(a => a.Status == ActivityStatus.Published)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.StartTime,
                    RegistrationCount = _context.ActivityRegistrations.Count(r => r.ActivityId == a.Id)
                })
                .OrderByDescending(a => a.RegistrationCount)
                .Take(10)
                .ToListAsync();

            // 活动分类统计
            var categoryStatistics = await _context.Activities
                .Include(a => a.Category)
                .GroupBy(a => a.Category!.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            // 每日注册统计（最近7天）
            var dailyRegistrations = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var date = now.Date.AddDays(-i);
                var nextDate = date.AddDays(1);
                var count = await _context.ActivityRegistrations
                    .CountAsync(r => r.CreatedAt >= date && r.CreatedAt < nextDate);
                
                dailyRegistrations.Add(new { Date = date.ToString("yyyy-MM-dd"), Count = count });
            }

            return Ok(new
            {
                BasicStatistics = new
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    TotalActivities = totalActivities,
                    PublishedActivities = publishedActivities,
                    TotalRegistrations = totalRegistrations
                },
                RecentStatistics = new
                {
                    NewUsersLast30Days = newUsersLast30Days,
                    NewActivitiesLast30Days = newActivitiesLast30Days,
                    NewRegistrationsLast30Days = newRegistrationsLast30Days
                },
                UserRoleDistribution = userRoleDistribution,
                PopularActivities = popularActivities,
                CategoryStatistics = categoryStatistics,
                DailyRegistrations = dailyRegistrations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统统计信息失败");
            return StatusCode(500, new { Message = "获取统计信息失败", Error = ex.Message });
        }
    }

    #endregion
} 