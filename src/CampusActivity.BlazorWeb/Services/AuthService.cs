using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.BlazorWeb.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClientFactory.CreateClient("CampusActivityAPI");
        _localStorage = localStorage;
        _authenticationStateProvider = authenticationStateProvider;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var json = JsonSerializer.Serialize(loginDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, _jsonOptions);

                if (authResponse != null)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.Token);
                    await _localStorage.SetItemAsync("user", authResponse.User);
                    
                    // 设置Authorization头
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", authResponse.Token);

                    // 通知认证状态已改变
                    await ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserAuthentication(authResponse.Token);

                    return authResponse;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
        }

        return null;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var json = JsonSerializer.Serialize(registerDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/register", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, _jsonOptions);

                if (authResponse != null)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.Token);
                    await _localStorage.SetItemAsync("user", authResponse.User);
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", authResponse.Token);

                    await ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserAuthentication(authResponse.Token);

                    return authResponse;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register error: {ex.Message}");
        }

        return null;
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("user");
        
        _httpClient.DefaultRequestHeaders.Authorization = null;
        
        await ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserLogout();
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<UserDto>("user");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            var changePasswordDto = new { CurrentPassword = currentPassword, NewPassword = newPassword };
            var json = JsonSerializer.Serialize(changePasswordDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/change-password", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Change password error: {ex.Message}");
            return false;
        }
    }
} 