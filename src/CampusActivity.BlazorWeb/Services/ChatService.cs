using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CampusActivity.Shared.DTOs;
using System.Threading;

namespace CampusActivity.BlazorWeb.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IUserContextService _userContextService;

        public ChatService(HttpClient httpClient, IConfiguration configuration, IUserContextService userContextService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _userContextService = userContextService;
        }

        public async Task<string> SendMessageAsync(string message, string? userId = null)
        {
            // 只调用后端API，不再直接调用OpenAI
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:7186/";
            var endpoint = $"{apiBaseUrl.TrimEnd('/')}/api/chat/send";

            var request = new
            {
                message = message,
                userId = userId
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var response = await _httpClient.PostAsync(endpoint, content, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<ChatResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return responseObj?.Message ?? responseContent;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"后端AI接口调用失败: {response.StatusCode} - {errorContent}");
                }
            }
            catch (TaskCanceledException)
            {
                throw new Exception("AI服务超时，请稍后重试");
            }
            catch (HttpRequestException)
            {
                throw new Exception("网络连接异常，请检查网络连接后重试");
            }
            catch (Exception ex)
            {
                throw new Exception($"AI助手调用失败: {ex.Message}");
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            // 检查后端API可用性
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:7186/";
                var endpoint = $"{apiBaseUrl.TrimEnd('/')}/api/chat/health";
                var response = await _httpClient.GetAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class ChatResponse
        {
            public string Message { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public bool Success { get; set; }
        }
    }
} 