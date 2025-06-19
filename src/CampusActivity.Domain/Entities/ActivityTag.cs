namespace CampusActivity.Domain.Entities;

public class ActivityTag : BaseEntity
{
    public int ActivityId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Color { get; set; }
    
    // 导航属性
    public virtual Activity Activity { get; set; } = null!;
} 