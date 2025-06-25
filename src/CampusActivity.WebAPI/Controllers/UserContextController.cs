using Microsoft.AspNetCore.Mvc;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserContextController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly ILogger<UserContextController> _logger;

    public UserContextController(IUserContextService userContextService, ILogger<UserContextController> logger)
    {
        _userContextService = userContextService;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserContextDto>> GetUserContext(int userId)
    {
        try
        {
            var context = await _userContextService.GetUserContextAsync(userId);
            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户上下文失败，用户ID: {UserId}", userId);
            return StatusCode(500, "获取用户上下文失败");
        }
    }

    [HttpGet("{userId}/summary")]
    public async Task<ActionResult<string>> GetUserContextSummary(int userId)
    {
        try
        {
            var summary = await _userContextService.GetUserContextSummaryAsync(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户上下文摘要失败，用户ID: {UserId}", userId);
            return StatusCode(500, "获取用户上下文摘要失败");
        }
    }
} 