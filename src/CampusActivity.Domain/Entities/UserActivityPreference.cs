namespace CampusActivity.Domain.Entities;

public class UserActivityPreference : BaseEntity
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public double Weight { get; set; } = 1.0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public virtual User User { get; set; } = null!;
    public virtual ActivityCategory Category { get; set; } = null!;
} 