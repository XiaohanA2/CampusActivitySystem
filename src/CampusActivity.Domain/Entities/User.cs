using CampusActivity.Shared.DTOs;

namespace CampusActivity.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    // 学生特有属性
    public string? StudentId { get; set; }
    public string? Major { get; set; }
    public int? Grade { get; set; }
    
    // 教师特有属性
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }

    // 导航属性
    public virtual ICollection<Activity> CreatedActivities { get; set; } = new List<Activity>();
    public virtual ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
    public virtual ICollection<UserActivityPreference> Preferences { get; set; } = new List<UserActivityPreference>();
} 