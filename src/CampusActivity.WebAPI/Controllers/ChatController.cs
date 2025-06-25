using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using CampusActivity.Application.Services;
using CampusActivity.Shared.DTOs;

namespace CampusActivity.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserContextService _userContextService;

        public ChatController(ILogger<ChatController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, IUserContextService userContextService)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _userContextService = userContextService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                _logger.LogInformation("收到AI聊天请求，UserId={UserId}, Message={Message}", request.UserId, request.Message);

                _logger.LogInformation("收到聊天请求: {Message}, UserId: {UserId}", request.Message, request.UserId);

                // 检查是否有配置的AI API
                var aiApiUrl = _configuration["AiSettings:ApiUrl"];
                var aiApiKey = _configuration["AiSettings:ApiKey"];
                var aiModel = _configuration["AiSettings:Model"];

                if (string.IsNullOrEmpty(aiApiUrl) || string.IsNullOrEmpty(aiApiKey))
                {
                    return BadRequest(new ChatResponse
                    {
                        Message = "OpenAI API配置不完整，请检查ApiUrl和ApiKey配置",
                        Timestamp = DateTime.Now,
                        Success = false
                    });
                }

                // 获取用户上下文
                string userContext = "";
                if (!string.IsNullOrEmpty(request.UserId) && int.TryParse(request.UserId, out int userIdInt))
                {
                    try
                    {
                        userContext = await _userContextService.GetUserContextSummaryAsync(userIdInt);
                        _logger.LogInformation("获取用户上下文成功，用户ID: {UserId}", userIdInt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取用户上下文失败，用户ID: {UserId}", userIdInt);
                        userContext = "用户信息获取失败，请基于通用校园活动信息回答。";
                    }
                }

                // 调用OpenAI API
                var response = await CallOpenAIAsync(request.Message, aiApiUrl, aiApiKey, aiModel, userContext);

                return Ok(new ChatResponse
                {
                    Message = response,
                    Timestamp = DateTime.Now,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理聊天请求时发生错误");
                return StatusCode(500, new ChatResponse
                {
                    Message = "OpenAI大模型调用失败，请稍后重试",
                    Timestamp = DateTime.Now,
                    Success = false
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.Now });
        }

        private async Task<string> CallOpenAIAsync(string message, string apiUrl, string apiKey, string model, string userContext = "")
        {
            // 构建系统提示词，包含用户上下文
            var systemPrompt = "你是校园活动系统的AI助手，专门帮助学生了解和管理校园活动。请用中文回答，回答要简洁明了，友好亲切。你可以帮助学生：1. 推荐适合的活动 2. 解答报名相关问题 3. 介绍热门活动 4. 帮助管理日程安排 5. 提供活动信息查询";
            
            if (!string.IsNullOrEmpty(userContext))
            {
                systemPrompt += $"\n\n当前用户信息：\n{userContext}\n\n请基于以上用户信息提供个性化的回答和建议。";
            }

            var request = new
            {
                model = model ?? "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = message }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 使用配置的HTTP客户端
            var httpClient = _httpClientFactory.CreateClient("OpenAI");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            _logger.LogInformation("开始调用OpenAI API，URL: {ApiUrl}, Model: {Model}", apiUrl, model);

            try
            {
                // 从配置文件获取超时设置
                var timeoutSeconds = _configuration.GetValue<int>("AiSettings:TimeoutSeconds", 30);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // 设置15秒超时
                
                var response = await httpClient.PostAsync(apiUrl, content, cts.Token);
                
                _logger.LogInformation("OpenAI API响应状态: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (responseObj.TryGetProperty("choices", out var choices) && 
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var messageObj) &&
                        messageObj.TryGetProperty("content", out var contentObj))
                    {
                        var aiResponse = contentObj.GetString() ?? "抱歉，我现在无法回答您的问题。";
                        _logger.LogInformation("OpenAI API调用成功，响应长度: {Length}", aiResponse.Length);
                        return aiResponse;
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI API响应格式异常: {ResponseContent}", responseContent);
                        throw new Exception("OpenAI API响应格式异常");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API调用失败，状态码: {StatusCode}, 错误内容: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    throw new Exception($"OpenAI API调用失败: {response.StatusCode}");
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI API调用超时");
                throw new Exception("OpenAI API调用超时，请稍后重试");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI API网络请求异常");
                throw new Exception("网络连接异常，请检查网络连接后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI API调用发生未知异常");
                throw new Exception("OpenAI大模型调用失败，请稍后重试");
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? UserId { get; set; }
    }

    public class ChatResponse
    {
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }
} 