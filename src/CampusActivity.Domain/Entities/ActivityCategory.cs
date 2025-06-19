namespace CampusActivity.Domain.Entities;

public class ActivityCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    
    // 导航属性
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
} 