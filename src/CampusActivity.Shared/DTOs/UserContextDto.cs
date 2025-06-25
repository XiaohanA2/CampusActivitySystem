namespace CampusActivity.Shared.DTOs;

public class UserContextDto
{
    public UserDto User { get; set; } = new();
    public List<ActivityRegistrationDto> RegisteredActivities { get; set; } = new();
    public List<ScheduleItemDto> UpcomingScheduleItems { get; set; } = new();
    public List<ScheduleItemDto> OverdueScheduleItems { get; set; } = new();
    public ScheduleStatisticsDto ScheduleStatistics { get; set; } = new();
} 