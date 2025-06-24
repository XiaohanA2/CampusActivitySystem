using System.Text;
using System.Text.Json;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public class ScheduleService : IScheduleService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ScheduleService(IHttpClientFactory httpClientFactory, IAuthService authService)
    {
        _httpClient = httpClientFactory.CreateClient("CampusActivityAPI");
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task SetAuthorizationHeader()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<ScheduleItemDto?> CreateScheduleItemAsync(CreateScheduleItemDto createDto)
    {
        try
        {
            Console.WriteLine("ScheduleService: 开始创建日程项...");
            Console.WriteLine($"ScheduleService: 请求URL: {_httpClient.BaseAddress}api/schedule");
            
            await SetAuthorizationHeader();
            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            Console.WriteLine($"ScheduleService: 请求数据: {json}");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/schedule", content);
            Console.WriteLine($"ScheduleService: 响应状态码: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ScheduleService: 响应内容: {responseContent}");
                return JsonSerializer.Deserialize<ScheduleItemDto>(responseContent, _jsonOptions);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ScheduleService: 错误响应: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ScheduleService: 创建日程项异常: {ex.Message}");
            Console.WriteLine($"ScheduleService: 异常堆栈: {ex.StackTrace}");
        }
        
        return null;
    }

    public async Task<ScheduleItemDto?> GetScheduleItemByIdAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/schedule/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ScheduleItemDto>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get schedule item error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<PagedResultDto<ScheduleItemDto>?> GetScheduleItemsAsync(ScheduleSearchDto searchDto)
    {
        try
        {
            await SetAuthorizationHeader();
            var queryParams = new List<string>();
            
            if (searchDto.StartDate.HasValue)
                queryParams.Add($"startDate={searchDto.StartDate.Value:yyyy-MM-dd}");
            if (searchDto.EndDate.HasValue)
                queryParams.Add($"endDate={searchDto.EndDate.Value:yyyy-MM-dd}");
            if (searchDto.Type.HasValue)
                queryParams.Add($"type={(int)searchDto.Type.Value}");
            if (searchDto.Priority.HasValue)
                queryParams.Add($"priority={(int)searchDto.Priority.Value}");
            if (searchDto.IsCompleted.HasValue)
                queryParams.Add($"isCompleted={searchDto.IsCompleted.Value}");
            if (!string.IsNullOrEmpty(searchDto.Keyword))
                queryParams.Add($"keyword={Uri.EscapeDataString(searchDto.Keyword)}");
            
            queryParams.Add($"pageIndex={searchDto.PageIndex}");
            queryParams.Add($"pageSize={searchDto.PageSize}");
            
            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"api/schedule?{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResultDto<ScheduleItemDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get schedule items error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<ScheduleItemDto?> UpdateScheduleItemAsync(int id, UpdateScheduleItemDto updateDto)
    {
        try
        {
            await SetAuthorizationHeader();
            var json = JsonSerializer.Serialize(updateDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/schedule/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ScheduleItemDto>(responseContent, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update schedule item error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> DeleteScheduleItemAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/schedule/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete schedule item error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ToggleCompletionAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsync($"api/schedule/{id}/toggle-completion", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Toggle completion error: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<ScheduleCalendarDto>?> GetCalendarViewAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/schedule/calendar?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ScheduleCalendarDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get calendar view error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<ScheduleStatisticsDto?> GetStatisticsAsync()
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/schedule/statistics");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ScheduleStatisticsDto>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get statistics error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> AddActivityToScheduleAsync(int activityId)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsync($"api/schedule/activities/{activityId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Add activity to schedule error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveActivityFromScheduleAsync(int activityId)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/schedule/activities/{activityId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remove activity from schedule error: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<ScheduleItemDto>?> GetUpcomingItemsAsync(int count = 10)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/schedule/upcoming?count={count}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ScheduleItemDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get upcoming items error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<IEnumerable<ScheduleItemDto>?> GetOverdueItemsAsync()
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/schedule/overdue");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ScheduleItemDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get overdue items error: {ex.Message}");
        }
        
        return null;
    }
} 