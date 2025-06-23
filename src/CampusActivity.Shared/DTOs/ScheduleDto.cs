using System.ComponentModel.DataAnnotations;
using CampusActivity.Shared.Enums;

namespace CampusActivity.Shared.DTOs;

public class ScheduleItemDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "标题不能为空")]
    [StringLength(200, ErrorMessage = "标题长度不能超过200个字符")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "描述长度不能超过1000个字符")]
    public string? Description { get; set; }
    
    [StringLength(200, ErrorMessage = "地点长度不能超过200个字符")]
    public string? Location { get; set; }
    
    [Required(ErrorMessage = "开始时间不能为空")]
    public DateTime StartTime { get; set; }
    
    [Required(ErrorMessage = "结束时间不能为空")]
    public DateTime EndTime { get; set; }
    
    public ScheduleItemType Type { get; set; } = ScheduleItemType.Personal;
    public ScheduleItemPriority Priority { get; set; } = ScheduleItemPriority.Medium;
    public string? Color { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string? Note { get; set; }
    public int? ActivityId { get; set; }
    
    // 关联的活动信息
    public string? ActivityTitle { get; set; }
    public string? ActivityLocation { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateScheduleItemDto
{
    [Required(ErrorMessage = "标题不能为空")]
    [StringLength(200, ErrorMessage = "标题长度不能超过200个字符")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "描述长度不能超过1000个字符")]
    public string? Description { get; set; }
    
    [StringLength(200, ErrorMessage = "地点长度不能超过200个字符")]
    public string? Location { get; set; }
    
    [Required(ErrorMessage = "开始时间不能为空")]
    public DateTime StartTime { get; set; }
    
    [Required(ErrorMessage = "结束时间不能为空")]
    public DateTime EndTime { get; set; }
    
    public ScheduleItemType Type { get; set; } = ScheduleItemType.Personal;
    public ScheduleItemPriority Priority { get; set; } = ScheduleItemPriority.Medium;
    public string? Color { get; set; }
    public string? Note { get; set; }
    public int? ActivityId { get; set; }
}

public class UpdateScheduleItemDto : CreateScheduleItemDto
{
    public int Id { get; set; }
    public bool IsCompleted { get; set; } = false;
}

public class ScheduleSearchDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ScheduleItemType? Type { get; set; }
    public ScheduleItemPriority? Priority { get; set; }
    public bool? IsCompleted { get; set; }
    public string? Keyword { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ScheduleCalendarDto
{
    public DateTime Date { get; set; }
    public List<ScheduleItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int PendingCount { get; set; }
}

public class ScheduleStatisticsDto
{
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int PendingItems { get; set; }
    public int OverdueItems { get; set; }
    public double CompletionRate { get; set; }
    public Dictionary<ScheduleItemType, int> ItemsByType { get; set; } = new();
    public Dictionary<ScheduleItemPriority, int> ItemsByPriority { get; set; } = new();
} 