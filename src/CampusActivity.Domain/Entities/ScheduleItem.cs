using CampusActivity.Shared.DTOs;
using CampusActivity.Shared.Enums;

namespace CampusActivity.Domain.Entities;

public class ScheduleItem : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ScheduleItemType Type { get; set; } = ScheduleItemType.Personal;
    public ScheduleItemPriority Priority { get; set; } = ScheduleItemPriority.Medium;
    public string? Color { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string? Note { get; set; }
    
    // 如果是活动相关的日程，关联活动ID
    public int? ActivityId { get; set; }
    
    // 导航属性
    public virtual User User { get; set; } = null!;
    public virtual Activity? Activity { get; set; }
} 