using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public class ActivityService : IActivityService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly JsonSerializerOptions _jsonOptions;

    public ActivityService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage)
    {
        _httpClient = httpClientFactory.CreateClient("CampusActivityAPI");
        _localStorage = localStorage;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<PagedResultDto<ActivityDto>?> GetActivitiesAsync(ActivitySearchDto searchDto)
    {
        try
        {
            await SetAuthorizationHeader();
            
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
                queryParams.Add($"keyword={Uri.EscapeDataString(searchDto.Keyword)}");
            if (searchDto.CategoryId.HasValue)
                queryParams.Add($"categoryId={searchDto.CategoryId}");
                    // 移除了Status参数，该字段已从DTO中删除
            if (searchDto.StartDate.HasValue)
                queryParams.Add($"startDate={searchDto.StartDate:yyyy-MM-dd}");
            if (searchDto.EndDate.HasValue)
                queryParams.Add($"endDate={searchDto.EndDate:yyyy-MM-dd}");
                    // 移除了Location参数，该字段已从DTO中删除
            if (searchDto.IsRegisterable.HasValue)
                queryParams.Add($"isRegisterable={searchDto.IsRegisterable}");
            
            queryParams.Add($"page={searchDto.Page}");
            queryParams.Add($"pageSize={searchDto.PageSize}");
            queryParams.Add($"sortBy={searchDto.SortBy}");
            queryParams.Add($"sortDescending={searchDto.SortDescending}");
            
            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"api/activities?{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResultDto<ActivityDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get activities error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<ActivityDto?> GetActivityByIdAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/activities/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ActivityDto>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get activity error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<ActivityDto?> CreateActivityAsync(CreateActivityDto createDto)
    {
        try
        {
            await SetAuthorizationHeader();
            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/activities", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ActivityDto>(responseContent, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Create activity error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<ActivityDto?> UpdateActivityAsync(int id, UpdateActivityDto updateDto)
    {
        try
        {
            await SetAuthorizationHeader();
            var json = JsonSerializer.Serialize(updateDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/activities/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ActivityDto>(responseContent, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update activity error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> DeleteActivityAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/activities/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete activity error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterForActivityAsync(int activityId, string? note = null)
    {
        try
        {
            await SetAuthorizationHeader();
            var registrationDto = new { Note = note };
            var json = JsonSerializer.Serialize(registrationDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/activities/{activityId}/register", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register for activity error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CancelRegistrationAsync(int activityId)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/activities/{activityId}/register");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cancel registration error: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<ActivityRegistrationDto>?> GetActivityRegistrationsAsync(int activityId)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/activities/{activityId}/registrations");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ActivityRegistrationDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get activity registrations error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<IEnumerable<ActivityCategoryDto>?> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/activities/categories");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ActivityCategoryDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get categories error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<IEnumerable<ActivityDto>?> GetPopularActivitiesAsync(int count = 10)
    {
        try
        {
            Console.WriteLine($"正在请求热门活动API: api/activities/popular?count={count}");
            var response = await _httpClient.GetAsync($"api/activities/popular?count={count}");
            
            Console.WriteLine($"API响应状态: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API响应内容: {content}");
                
                var activities = JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(content, _jsonOptions);
                Console.WriteLine($"反序列化后的活动数量: {activities?.Count() ?? 0}");
                return activities;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API请求失败: {response.StatusCode}, 错误内容: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get popular activities error: {ex.Message}");
            Console.WriteLine($"Exception details: {ex}");
        }
        
        return null;
    }

    private async Task SetAuthorizationHeader()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
} 