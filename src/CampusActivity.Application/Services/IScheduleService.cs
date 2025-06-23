using CampusActivity.Shared.DTOs;

namespace CampusActivity.Application.Services;

public interface IScheduleService
{
    Task<ScheduleItemDto> CreateScheduleItemAsync(CreateScheduleItemDto createDto, int userId);
    Task<ScheduleItemDto?> GetScheduleItemByIdAsync(int id, int userId);
    Task<PagedResultDto<ScheduleItemDto>> GetUserScheduleItemsAsync(ScheduleSearchDto searchDto, int userId);
    Task<ScheduleItemDto> UpdateScheduleItemAsync(int id, UpdateScheduleItemDto updateDto, int userId);
    Task<bool> DeleteScheduleItemAsync(int id, int userId);
    Task<bool> ToggleScheduleItemCompletionAsync(int id, int userId);
    Task<IEnumerable<ScheduleCalendarDto>> GetCalendarViewAsync(DateTime startDate, DateTime endDate, int userId);
    Task<ScheduleStatisticsDto> GetScheduleStatisticsAsync(int userId);
    Task<bool> AddActivityToScheduleAsync(int activityId, int userId);
    Task<bool> RemoveActivityFromScheduleAsync(int activityId, int userId);
    Task<IEnumerable<ScheduleItemDto>> GetUpcomingItemsAsync(int userId, int count = 10);
    Task<IEnumerable<ScheduleItemDto>> GetOverdueItemsAsync(int userId);
} 