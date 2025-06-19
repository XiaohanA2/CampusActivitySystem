using System.ComponentModel.DataAnnotations;

namespace CampusActivity.Shared.DTOs;

public class ActivityDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "活动标题不能为空")]
    [StringLength(200, ErrorMessage = "活动标题长度不能超过200个字符")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "活动描述不能为空")]
    [StringLength(2000, ErrorMessage = "活动描述长度不能超过2000个字符")]
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "活动地点不能为空")]
    [StringLength(200, ErrorMessage = "活动地点长度不能超过200个字符")]
    public string Location { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "开始时间不能为空")]
    public DateTime StartTime { get; set; }
    
    [Required(ErrorMessage = "结束时间不能为空")]
    public DateTime EndTime { get; set; }
    
    [Required(ErrorMessage = "报名截止时间不能为空")]
    public DateTime RegistrationDeadline { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "最大参与人数必须大于0")]
    public int MaxParticipants { get; set; }
    
    public int CurrentParticipants { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public ActivityStatus Status { get; set; }
    
    public int CategoryId { get; set; }
    
    public string CategoryName { get; set; } = string.Empty;
    
    public int CreatedBy { get; set; }
    
    public string CreatedByName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public bool IsRegistered { get; set; }
    
    public double? RecommendationScore { get; set; }
    
    public List<string> Tags { get; set; } = new();
}

public class CreateActivityDto
{
    [Required(ErrorMessage = "活动标题不能为空")]
    [StringLength(200, ErrorMessage = "活动标题长度不能超过200个字符")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "活动描述不能为空")]
    [StringLength(2000, ErrorMessage = "活动描述长度不能超过2000个字符")]
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "活动地点不能为空")]
    [StringLength(200, ErrorMessage = "活动地点长度不能超过200个字符")]
    public string Location { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "开始时间不能为空")]
    public DateTime StartTime { get; set; }
    
    [Required(ErrorMessage = "结束时间不能为空")]
    public DateTime EndTime { get; set; }
    
    [Required(ErrorMessage = "报名截止时间不能为空")]
    public DateTime RegistrationDeadline { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "最大参与人数必须大于0")]
    public int MaxParticipants { get; set; }
    
    public string? ImageUrl { get; set; }
    
    [Required(ErrorMessage = "请选择活动分类")]
    public int CategoryId { get; set; }
    
    public List<string> Tags { get; set; } = new();
}

public class UpdateActivityDto : CreateActivityDto
{
    public int Id { get; set; }
    public ActivityStatus Status { get; set; }
}

public class ActivityCategoryDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "分类名称不能为空")]
    [StringLength(100, ErrorMessage = "分类名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "分类描述长度不能超过500个字符")]
    public string? Description { get; set; }
    
    public string? IconUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int ActivityCount { get; set; }
}

public class ActivityRegistrationDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime RegistrationTime { get; set; }
    public RegistrationStatus Status { get; set; }
    public string? Note { get; set; }
}

public class ActivitySearchDto
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public ActivityStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public bool? IsRegisterable { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "StartTime";
    public bool SortDescending { get; set; } = false;
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

public enum ActivityStatus
{
    Draft = 0,      // 草稿
    Published = 1,  // 已发布
    Cancelled = 2,  // 已取消
    Completed = 3   // 已完成
}

public enum RegistrationStatus
{
    Registered = 1,    // 已报名
    Cancelled = 2,     // 已取消
    Attended = 3,      // 已参加
    Absent = 4         // 缺席
} 