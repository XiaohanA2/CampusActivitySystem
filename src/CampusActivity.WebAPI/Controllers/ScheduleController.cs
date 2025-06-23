using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusActivity.Application.Services;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        IScheduleService scheduleService,
        ILogger<ScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// 创建日程项
    /// </summary>
    /// <param name="createDto">创建日程项信息</param>
    /// <returns>创建的日程项</returns>
    [HttpPost]
    public async Task<ActionResult<ScheduleItemDto>> CreateScheduleItem([FromBody] CreateScheduleItemDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var scheduleItem = await _scheduleService.CreateScheduleItemAsync(createDto, userId);
            return Ok(scheduleItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建日程项失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取日程项详情
    /// </summary>
    /// <param name="id">日程项ID</param>
    /// <returns>日程项详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleItemDto>> GetScheduleItem(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var scheduleItem = await _scheduleService.GetScheduleItemByIdAsync(id, userId);
            
            if (scheduleItem == null)
                return NotFound(new { message = "日程项不存在" });
                
            return Ok(scheduleItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取日程项详情失败，ID: {Id}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取用户日程列表
    /// </summary>
    /// <param name="searchDto">搜索条件</param>
    /// <returns>日程列表</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ScheduleItemDto>>> GetScheduleItems([FromQuery] ScheduleSearchDto searchDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _scheduleService.GetUserScheduleItemsAsync(searchDto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取日程列表失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新日程项
    /// </summary>
    /// <param name="id">日程项ID</param>
    /// <param name="updateDto">更新信息</param>
    /// <returns>更新后的日程项</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ScheduleItemDto>> UpdateScheduleItem(int id, [FromBody] UpdateScheduleItemDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            updateDto.Id = id;
            var scheduleItem = await _scheduleService.UpdateScheduleItemAsync(id, updateDto, userId);
            return Ok(scheduleItem);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新日程项失败，ID: {Id}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除日程项
    /// </summary>
    /// <param name="id">日程项ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScheduleItem(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _scheduleService.DeleteScheduleItemAsync(id, userId);
            
            if (success)
                return Ok(new { message = "删除成功" });
            else
                return NotFound(new { message = "日程项不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除日程项失败，ID: {Id}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 切换日程项完成状态
    /// </summary>
    /// <param name="id">日程项ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/toggle-completion")]
    public async Task<IActionResult> ToggleCompletion(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _scheduleService.ToggleScheduleItemCompletionAsync(id, userId);
            
            if (success)
                return Ok(new { message = "状态更新成功" });
            else
                return NotFound(new { message = "日程项不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换日程项完成状态失败，ID: {Id}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取日历视图
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>日历数据</returns>
    [HttpGet("calendar")]
    public async Task<ActionResult<IEnumerable<ScheduleCalendarDto>>> GetCalendarView(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var userId = GetCurrentUserId();
            var calendarData = await _scheduleService.GetCalendarViewAsync(startDate, endDate, userId);
            return Ok(calendarData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取日历视图失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取日程统计
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ScheduleStatisticsDto>> GetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var statistics = await _scheduleService.GetScheduleStatisticsAsync(userId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取日程统计失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 添加活动到日程表
    /// </summary>
    /// <param name="activityId">活动ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("activities/{activityId}")]
    public async Task<IActionResult> AddActivityToSchedule(int activityId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _scheduleService.AddActivityToScheduleAsync(activityId, userId);
            
            if (success)
                return Ok(new { message = "活动已添加到日程表" });
            else
                return BadRequest(new { message = "添加失败" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加活动到日程表失败，活动ID: {ActivityId}", activityId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 从日程表移除活动
    /// </summary>
    /// <param name="activityId">活动ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("activities/{activityId}")]
    public async Task<IActionResult> RemoveActivityFromSchedule(int activityId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _scheduleService.RemoveActivityFromScheduleAsync(activityId, userId);
            
            if (success)
                return Ok(new { message = "活动已从日程表移除" });
            else
                return NotFound(new { message = "日程项不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从日程表移除活动失败，活动ID: {ActivityId}", activityId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取即将到来的日程项
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>即将到来的日程项</returns>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<ScheduleItemDto>>> GetUpcomingItems([FromQuery] int count = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var items = await _scheduleService.GetUpcomingItemsAsync(userId, count);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取即将到来的日程项失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取逾期日程项
    /// </summary>
    /// <returns>逾期日程项</returns>
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<ScheduleItemDto>>> GetOverdueItems()
    {
        try
        {
            var userId = GetCurrentUserId();
            var items = await _scheduleService.GetOverdueItemsAsync(userId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取逾期日程项失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("无效的用户身份");
    }
} 