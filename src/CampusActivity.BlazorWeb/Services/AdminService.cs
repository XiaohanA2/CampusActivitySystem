using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public class AdminService : IAdminService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AdminService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage, ILogger<AdminService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CampusActivityAPI");
        _localStorage = localStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    #region 用户管理

    public async Task<PagedResult<UserDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, string? search = null, UserRole? role = null)
    {
        try
        {
            await SetAuthorizationHeader();
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (role.HasValue)
                queryParams.Add($"role={role.Value}");

            var query = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"api/admin/users?{query}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);

                var pagedResult = new PagedResult<UserDto>
                {
                    Items = JsonSerializer.Deserialize<List<UserDto>>(result.GetProperty("users").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new(),
                    TotalCount = result.GetProperty("totalCount").GetInt32(),
                    Page = result.GetProperty("page").GetInt32(),
                    PageSize = result.GetProperty("pageSize").GetInt32(),
                    TotalPages = result.GetProperty("totalPages").GetInt32()
                };

                return pagedResult;
            }

            _logger.LogError($"获取用户列表失败: {response.StatusCode}");
            return new PagedResult<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表时发生异常");
            return new PagedResult<UserDto>();
        }
    }

    public async Task<bool> UpdateUserStatusAsync(int id, bool isActive)
    {
        try
        {
            await SetAuthorizationHeader();
            var content = new StringContent(JsonSerializer.Serialize(isActive), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/admin/users/{id}/status", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新用户状态失败，用户ID: {id}");
            return false;
        }
    }

    public async Task<bool> ResetUserPasswordAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsync($"api/admin/users/{id}/reset-password", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"重置用户密码失败，用户ID: {id}");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/admin/users/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除用户失败，用户ID: {id}");
            return false;
        }
    }

    #endregion

    #region 活动管理

    public async Task<PagedResult<AdminActivityDto>> GetAllActivitiesAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, string? status = null)
    {
        try
        {
            await SetAuthorizationHeader();
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (categoryId.HasValue)
                queryParams.Add($"categoryId={categoryId.Value}");

            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={status}");

            var query = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"api/admin/activities?{query}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);

                var pagedResult = new PagedResult<AdminActivityDto>
                {
                    Items = JsonSerializer.Deserialize<List<AdminActivityDto>>(result.GetProperty("activities").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new(),
                    TotalCount = result.GetProperty("totalCount").GetInt32(),
                    Page = result.GetProperty("page").GetInt32(),
                    PageSize = result.GetProperty("pageSize").GetInt32(),
                    TotalPages = result.GetProperty("totalPages").GetInt32()
                };

                return pagedResult;
            }

            _logger.LogError($"获取活动列表失败: {response.StatusCode}");
            return new PagedResult<AdminActivityDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动列表时发生异常");
            return new PagedResult<AdminActivityDto>();
        }
    }

    public async Task<bool> ForceDeleteActivityAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/admin/activities/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除活动失败，活动ID: {id}");
            return false;
        }
    }

    public async Task<AdminActivityDto?> GetActivityByIdAsync(int id)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/admin/activities/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdminActivityDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result;
            }
            _logger.LogError($"获取活动详情失败: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动详情时发生异常");
            return null;
        }
    }

    public async Task<bool> UpdateActivityAsync(AdminActivityDto activity)
    {
        try
        {
            await SetAuthorizationHeader();
            var content = new StringContent(JsonSerializer.Serialize(activity), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/admin/activities/{activity.Id}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新活动失败，活动ID: {activity.Id}");
            return false;
        }
    }

    public async Task<bool> CreateActivityAsync(AdminActivityDto activity)
    {
        try
        {
            _logger.LogInformation("=== AdminService.CreateActivityAsync 开始 ===");
            _logger.LogInformation($"活动数据: {JsonSerializer.Serialize(activity)}");
            
            await SetAuthorizationHeader();
            var content = new StringContent(JsonSerializer.Serialize(activity), Encoding.UTF8, "application/json");
            
            _logger.LogInformation("发送POST请求到: api/admin/activities");
            var response = await _httpClient.PostAsync("api/admin/activities", content);
            
            _logger.LogInformation($"HTTP响应状态: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"成功响应内容: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"失败响应内容: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建活动失败");
            return false;
        }
    }

    #endregion

    #region 系统统计

    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/admin/statistics");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                
                var statistics = new SystemStatisticsDto();
                
                // 基础统计
                if (result.TryGetProperty("basicStatistics", out var basicStats))
                {
                    statistics.BasicStatistics = JsonSerializer.Deserialize<BasicStatistics>(basicStats.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                
                // 最近统计
                if (result.TryGetProperty("recentStatistics", out var recentStats))
                {
                    statistics.RecentStatistics = JsonSerializer.Deserialize<RecentStatistics>(recentStats.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                
                // 用户角色分布
                if (result.TryGetProperty("userRoleDistribution", out var roleDist))
                {
                    statistics.UserRoleDistribution = JsonSerializer.Deserialize<List<RoleDistribution>>(roleDist.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                
                // 热门活动
                if (result.TryGetProperty("popularActivities", out var popularActivities))
                {
                    statistics.PopularActivities = JsonSerializer.Deserialize<List<PopularActivity>>(popularActivities.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                
                // 活动分类统计
                if (result.TryGetProperty("categoryStatistics", out var categoryStats))
                {
                    statistics.CategoryStatistics = JsonSerializer.Deserialize<List<CategoryStatistic>>(categoryStats.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                
                // 每日注册统计
                if (result.TryGetProperty("dailyRegistrations", out var dailyRegs))
                {
                    statistics.DailyRegistrations = JsonSerializer.Deserialize<List<DailyRegistration>>(dailyRegs.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                
                return statistics;
            }

            _logger.LogError($"获取系统统计信息失败: {response.StatusCode}");
            return new SystemStatisticsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统统计信息时发生异常");
            return new SystemStatisticsDto();
        }
    }

    #endregion

    private async Task SetAuthorizationHeader()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置认证头失败");
        }
    }
} 