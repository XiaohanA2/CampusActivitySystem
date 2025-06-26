using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CampusActivity.Application.Services;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(IRecommendationService recommendationService, ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取个性化推荐活动
    /// </summary>
    /// <param name="count">推荐数量</param>
    /// <returns>推荐活动列表</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetRecommendations([FromQuery] int count = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recommendations = await _recommendationService.GetRecommendedActivitiesAsync(userId, count);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推荐活动失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取协同过滤推荐
    /// </summary>
    /// <param name="count">推荐数量</param>
    /// <returns>推荐活动列表</returns>
    [HttpGet("collaborative")]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetCollaborativeRecommendations([FromQuery] int count = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recommendations = await _recommendationService.GetCollaborativeRecommendationsAsync(userId, count);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取协同过滤推荐失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取基于内容的推荐
    /// </summary>
    /// <param name="count">推荐数量</param>
    /// <returns>推荐活动列表</returns>
    [HttpGet("content-based")]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetContentBasedRecommendations([FromQuery] int count = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recommendations = await _recommendationService.GetContentBasedRecommendationsAsync(userId, count);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取基于内容的推荐失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取用户偏好设置
    /// </summary>
    /// <returns>用户偏好列表</returns>
    [HttpGet("preferences")]
    public async Task<ActionResult<IEnumerable<UserActivityPreferenceDto>>> GetUserPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            var preferences = await _recommendationService.GetUserPreferencesAsync(userId);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户偏好失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新用户偏好
    /// </summary>
    /// <param name="updateDto">偏好更新信息</param>
    /// <returns>操作结果</returns>
    [HttpPost("preferences")]
    public async Task<IActionResult> UpdateUserPreference([FromBody] UpdatePreferenceDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _recommendationService.UpdateUserPreferencesAsync(userId, updateDto.CategoryId, updateDto.Weight);
            return Ok(new { message = "偏好更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户偏好失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 重新计算推荐（强制刷新）
    /// </summary>
    /// <returns>操作结果</returns>
    [HttpPost("recalculate")]
    public async Task<IActionResult> RecalculateRecommendations()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _recommendationService.RecalculateRecommendationsAsync(userId);
            return Ok(new { message = "推荐重新计算完成" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新计算推荐失败");
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
        
        // 记录调试信息
        _logger.LogWarning("无法获取用户ID，用户身份信息：{Claims}", 
            string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
        
        throw new UnauthorizedAccessException("无效的用户身份");
    }
}

public class UpdatePreferenceDto
{
    public int CategoryId { get; set; }
    public double Weight { get; set; }
} 