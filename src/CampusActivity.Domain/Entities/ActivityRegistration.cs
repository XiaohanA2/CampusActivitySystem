using CampusActivity.Shared.DTOs;

namespace CampusActivity.Domain.Entities;

public class ActivityRegistration : BaseEntity
{
    public int ActivityId { get; set; }
    public int UserId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;
    public string? Note { get; set; }
    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public virtual Activity Activity { get; set; } = null!;
    public virtual User User { get; set; } = null!;
} 