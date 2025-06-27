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
    private readonly IWebHostEnvironment _environment;

    public ActivitiesController(IActivityService activityService, ILogger<ActivitiesController> logger, IWebHostEnvironment environment)
    {
        _activityService = activityService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 获取活动列表（分页搜索）
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="categoryId">分类ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="isRegisterable">是否可报名</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="page">页码（替代参数）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="sortBy">排序字段</param>
    /// <param name="sortDescending">是否降序</param>
    /// <returns>活动列表</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ActivityDto>>> GetActivities(
        [FromQuery] string? keyword = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? isRegisterable = null,
        [FromQuery] int? pageIndex = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = 12,
        [FromQuery] string sortBy = "StartTime",
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            // 处理页码参数兼容性
            var actualPage = pageIndex ?? page ?? 1;
            
            var searchDto = new ActivitySearchDto
            {
                Keyword = keyword,
                CategoryId = categoryId,
                StartDate = startDate,
                EndDate = endDate,
                IsRegisterable = isRegisterable,
                Page = actualPage,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };
            
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

    /// <summary>
    /// 上传活动图片
    /// </summary>
    /// <param name="file">图片文件</param>
    /// <returns>图片URL</returns>
    [HttpPost("upload-image")]
    [Authorize(Roles = $"{AppConstants.Roles.Teacher},{AppConstants.Roles.Admin}")]
    public async Task<ActionResult> UploadImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "请选择图片文件" });

            // 检查文件类型
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { message = "只支持 JPEG、PNG、GIF、WebP 格式的图片" });

            // 检查文件大小 (最大5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "图片文件大小不能超过5MB" });

            // 创建上传目录
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "activities");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            // 生成唯一文件名
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 返回相对URL，注意属性名要与前端匹配
            var imageUrl = $"/uploads/activities/{fileName}";
            
            _logger.LogInformation($"图片上传成功: {imageUrl}");
            return Ok(new { ImageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图片上传失败");
            return StatusCode(500, new { message = "图片上传失败" });
        }
    }

    /// <summary>
    /// 删除活动图片
    /// </summary>
    /// <param name="imageUrl">图片URL</param>
    /// <returns>操作结果</returns>
    [HttpDelete("delete-image")]
    [Authorize(Roles = $"{AppConstants.Roles.Teacher},{AppConstants.Roles.Admin}")]
    public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
                return BadRequest(new { message = "图片URL不能为空" });

            // 只处理本地上传的图片
            if (!imageUrl.StartsWith("/uploads/activities/"))
                return BadRequest(new { message = "只能删除本地上传的图片" });

            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "activities", fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation($"图片删除成功: {imageUrl}");
                return Ok(new { message = "图片删除成功" });
            }
            else
            {
                return NotFound(new { message = "图片文件不存在" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除图片失败: {imageUrl}");
            return StatusCode(500, new { message = "删除图片失败" });
        }
    }

    /// <summary>
    /// 获取随机示例图片
    /// </summary>
    /// <param name="category">活动分类名称（可选）</param>
    /// <returns>随机图片URL</returns>
    [HttpGet("random-image")]
    public ActionResult GetRandomImage([FromQuery] string? category = null)
    {
        try
        {
            var random = new Random();
            var width = 600;
            var height = 400;
            var seed = random.Next(1, 1000);

            // 根据数据库中的分类名称选择主题关键词
            var themeKeywords = category switch
            {
                "学术讲座" => new[] { "books", "study", "library", "education", "science", "research", "lecture" },
                "文艺演出" => new[] { "art", "music", "theater", "painting", "culture", "performance", "stage" },
                "体育竞技" => new[] { "sports", "fitness", "running", "basketball", "football", "exercise", "competition" },
                "社会实践" => new[] { "volunteer", "help", "community", "service", "care", "charity", "social" },
                "创新创业" => new[] { "technology", "computer", "coding", "innovation", "digital", "tech", "startup" },
                "交流参观" => new[] { "group", "team", "meeting", "community", "visit", "exchange", "tour" },
                _ => new[] { "campus", "students", "university", "activity", "event", "college" }
            };

            var selectedTheme = themeKeywords[random.Next(themeKeywords.Length)];
            var imageUrl = $"https://picsum.photos/seed/{selectedTheme}-{seed}/{width}/{height}";

            return Ok(new { ImageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取随机图片失败");
            return StatusCode(500, new { message = "获取随机图片失败" });
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