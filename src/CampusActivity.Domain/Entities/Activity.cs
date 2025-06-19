using CampusActivity.Shared.DTOs;

namespace CampusActivity.Domain.Entities;

public class Activity : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public string? ImageUrl { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Draft;
    
    // 外键
    public int CategoryId { get; set; }
    
    // 导航属性
    public virtual ActivityCategory Category { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
    public virtual ICollection<ActivityTag> Tags { get; set; } = new List<ActivityTag>();
    public virtual ICollection<ActivityRecommendation> Recommendations { get; set; } = new List<ActivityRecommendation>();
} 