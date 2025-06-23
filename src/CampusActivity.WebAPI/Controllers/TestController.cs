using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusActivity.Infrastructure.Data;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(ApplicationDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<object> Get()
    {
        return Ok(new
        {
            Message = "API连接正常",
            Timestamp = DateTime.Now,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Assembly = typeof(TestController).Assembly.GetName().Name
        });
    }

    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.Now,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId
        });
    }

    [HttpGet("database")]
    public async Task<ActionResult<object>> TestDatabase()
    {
        try
        {
            // 测试数据库连接
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(500, new
                {
                    Status = "Database Connection Failed",
                    Message = "无法连接到数据库",
                    Timestamp = DateTime.Now
                });
            }

            // 测试基本查询
            var userCount = await _context.Users.CountAsync();
            var categoryCount = await _context.ActivityCategories.CountAsync();
            var activityCount = await _context.Activities.CountAsync();

            return Ok(new
            {
                Status = "Database Connected",
                Message = "数据库连接正常",
                Timestamp = DateTime.Now,
                Statistics = new
                {
                    Users = userCount,
                    Categories = categoryCount,
                    Activities = activityCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库测试失败");
            return StatusCode(500, new
            {
                Status = "Database Error",
                Message = ex.Message,
                Timestamp = DateTime.Now
            });
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<object>> TestCategories()
    {
        try
        {
            var categories = await _context.ActivityCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.Description })
                .ToListAsync();

            return Ok(new
            {
                Status = "Success",
                Count = categories.Count,
                Categories = categories,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动分类失败");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.Now
            });
        }
    }

    [HttpGet("activities")]
    public async Task<ActionResult<object>> TestActivities()
    {
        try
        {
            var activities = await _context.Activities
                .Where(a => a.Status == ActivityStatus.Published)
                .Select(a => new { a.Id, a.Title, Status = a.Status.ToString(), a.CurrentParticipants })
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                Status = "Success",
                Count = activities.Count,
                Activities = activities,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动列表失败");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.Now
            });
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> GetDatabaseStatus()
    {
        try
        {
            var userCount = await _context.Users.CountAsync();
            var categoryCount = await _context.ActivityCategories.CountAsync();
            var activityCount = await _context.Activities.CountAsync();
            var registrationCount = await _context.ActivityRegistrations.CountAsync();

            var recentActivities = await _context.Activities
                .Where(a => a.Status == ActivityStatus.Published)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new { a.Id, a.Title, a.Location, a.StartTime, a.CurrentParticipants, a.MaxParticipants })
                .ToListAsync();

            return Ok(new
            {
                Status = "Database Status",
                Timestamp = DateTime.Now,
                Statistics = new
                {
                    Users = userCount,
                    Categories = categoryCount,
                    Activities = activityCount,
                    Registrations = registrationCount
                },
                RecentActivities = recentActivities,
                HasData = activityCount > 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据库状态失败");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.Now
            });
        }
    }
} 