using System.Text.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public class RecommendationService : IRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly JsonSerializerOptions _jsonOptions;

    public RecommendationService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage)
    {
        _httpClient = httpClientFactory.CreateClient("CampusActivityAPI");
        _localStorage = localStorage;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<IEnumerable<ActivityDto>?> GetRecommendationsAsync(int count = 10)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/recommendations?count={count}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(content, _jsonOptions);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Get recommendations failed with status {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get recommendations error: {ex.Message}");
            Console.WriteLine($"Full exception: {ex}");
        }
        
        return null;
    }

    public async Task<IEnumerable<ActivityDto>?> GetCollaborativeRecommendationsAsync(int count = 10)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/recommendations/collaborative?count={count}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get collaborative recommendations error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<IEnumerable<ActivityDto>?> GetContentBasedRecommendationsAsync(int count = 10)
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/recommendations/content-based?count={count}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ActivityDto>>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get content-based recommendations error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> RecalculateRecommendationsAsync()
    {
        try
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsync("api/recommendations/recalculate", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Recalculate recommendations error: {ex.Message}");
            return false;
        }
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