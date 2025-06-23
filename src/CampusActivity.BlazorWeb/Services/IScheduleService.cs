using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public interface IScheduleService
{
    Task<ScheduleItemDto?> CreateScheduleItemAsync(CreateScheduleItemDto createDto);
    Task<ScheduleItemDto?> GetScheduleItemByIdAsync(int id);
    Task<PagedResultDto<ScheduleItemDto>?> GetScheduleItemsAsync(ScheduleSearchDto searchDto);
    Task<ScheduleItemDto?> UpdateScheduleItemAsync(int id, UpdateScheduleItemDto updateDto);
    Task<bool> DeleteScheduleItemAsync(int id);
    Task<bool> ToggleCompletionAsync(int id);
    Task<IEnumerable<ScheduleCalendarDto>?> GetCalendarViewAsync(DateTime startDate, DateTime endDate);
    Task<ScheduleStatisticsDto?> GetStatisticsAsync();
    Task<bool> AddActivityToScheduleAsync(int activityId);
    Task<bool> RemoveActivityFromScheduleAsync(int activityId);
    Task<IEnumerable<ScheduleItemDto>?> GetUpcomingItemsAsync(int count = 10);
    Task<IEnumerable<ScheduleItemDto>?> GetOverdueItemsAsync();
} 