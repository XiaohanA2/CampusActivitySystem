using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CampusActivity.Application.Services;
using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Constants;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(IActivityService activityService, ILogger<ActivitiesController> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    /// <summary>
    /// 获取活动列表（分页搜索）
    /// </summary>
    /// <param name="searchDto">搜索条件</param>
    /// <returns>活动列表</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ActivityDto>>> GetActivities([FromQuery] ActivitySearchDto searchDto)
    {
        try
        {
            var result = await _activityService.GetActivitiesAsync(searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动列表失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取活动详情
    /// </summary>
    /// <param name="id">活动ID</param>
    /// <returns>活动详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ActivityDto>> GetActivity(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserIdOrDefault();
            var activity = await _activityService.GetActivityByIdAsync(id, currentUserId);
            
            if (activity == null)
                return NotFound(new { message = "活动不存在" });
                
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动详情失败，活动ID: {ActivityId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 创建活动（教师/管理员）
    /// </summary>
    /// <param name="createDto">活动创建信息</param>
    /// <returns>创建的活动</returns>
    [HttpPost]
    [Authorize(Roles = $"{AppConstants.Roles.Teacher},{AppConstants.Roles.Admin}")]
    public async Task<ActionResult<ActivityDto>> CreateActivity([FromBody] CreateActivityDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var activity = await _activityService.CreateActivityAsync(createDto, userId);
            return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建活动失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新活动（教师/管理员）
    /// </summary>
    /// <param name="id">活动ID</param>
    /// <param name="updateDto">活动更新信息</param>
    /// <returns>更新后的活动</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{AppConstants.Roles.Teacher},{AppConstants.Roles.Admin}")]
    public async Task<ActionResult<ActivityDto>> UpdateActivity(int id, [FromBody] UpdateActivityDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            updateDto.Id = id;
            var activity = await _activityService.UpdateActivityAsync(id, updateDto, userId);
            return Ok(activity);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新活动失败，活动ID: {ActivityId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除活动（管理员）
    /// </summary>
    /// <param name="id">活动ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        try
        {
            var success = await _activityService.DeleteActivityAsync(id);
            if (success)
                return Ok(new { message = "活动删除成功" });
            else
                return NotFound(new { message = "活动不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除活动失败，活动ID: {ActivityId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 报名活动（学生）
    /// </summary>
    /// <param name="id">活动ID</param>
    /// <param name="registrationDto">报名信息</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/register")]
    [Authorize(Roles = AppConstants.Roles.Student)]
    public async Task<IActionResult> RegisterForActivity(int id, [FromBody] ActivityRegistrationRequestDto registrationDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _activityService.RegisterForActivityAsync(id, userId, registrationDto.Note);
            
            if (success)
                return Ok(new { message = "报名成功" });
            else
                return BadRequest(new { message = "报名失败" });
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
            _logger.LogError(ex, "报名活动失败，活动ID: {ActivityId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 取消报名（学生）
    /// </summary>
    /// <param name="id">活动ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}/register")]
    [Authorize(Roles = AppConstants.Roles.Student)]
    public async Task<IActionResult> CancelRegistration(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _activityService.CancelRegistrationAsync(id, userId);
            
            if (success)
                return Ok(new { message = "取消报名成功" });
            else
                return BadRequest(new { message = "取消报名失败" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消报名失败，活动ID: {ActivityId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取活动报名列表（教师/管理员）
    /// </summary>
    /// <param name="id">活动ID</param>
    /// <returns>报名列表</returns>
    [HttpGet("{id}/registrations")]
    [Authorize(Roles = $"{AppConstants.Roles.Teacher},{AppConstants.Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<ActivityRegistrationDto>>> GetActivityRegistrations(int id)
    {
        try
        {
            var registrations = await _activityService.GetActivityRegistrationsAsync(id);
            return Ok(registrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动报名列表失败，活动ID: {ActivityId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取我报名的活动
    /// </summary>
    /// <returns>已报名活动列表</returns>
    [HttpGet("my-registrations")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetMyRegistrations()
    {
        try
        {
            var userId = GetCurrentUserId();
            var activities = await _activityService.GetUserRegisteredActivitiesAsync(userId);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取我的报名活动失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取活动分类
    /// </summary>
    /// <returns>分类列表</returns>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<ActivityCategoryDto>>> GetCategories()
    {
        try
        {
            var categories = await _activityService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动分类失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取热门活动
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>热门活动列表</returns>
    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetPopularActivities([FromQuery] int count = 10)
    {
        try
        {
            var activities = await _activityService.GetPopularActivitiesAsync(count);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门活动失败");
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

    private int? GetCurrentUserIdOrDefault()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }
}

public class ActivityRegistrationRequestDto
{
    public string? Note { get; set; }
} 