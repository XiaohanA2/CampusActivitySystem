namespace CampusActivity.Domain.Entities;

public class ActivityRecommendation : BaseEntity
{
    public int ActivityId { get; set; }
    public int UserId { get; set; }
    public double Score { get; set; }
    public string? Reason { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public virtual Activity Activity { get; set; } = null!;
    public virtual User User { get; set; } = null!;
} 