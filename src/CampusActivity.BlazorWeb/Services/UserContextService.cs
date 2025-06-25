using CampusActivity.Shared.DTOs;
using System.Net.Http.Json;

namespace CampusActivity.BlazorWeb.Services;

public class UserContextService : IUserContextService
{
    private readonly HttpClient _httpClient;

    public UserContextService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserContextDto> GetUserContextAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/usercontext/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserContextDto>() ?? new UserContextDto();
            }
        }
        catch
        {
            // 如果API调用失败，返回空上下文
        }
        
        return new UserContextDto();
    }

    public async Task<string> GetUserContextSummaryAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/usercontext/{userId}/summary");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
        catch
        {
            // 如果API调用失败，返回默认信息
        }
        
        return "用户信息获取失败，请基于通用校园活动信息回答。";
    }
} 