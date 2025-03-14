using Microsoft.Extensions.Options;
using SchedulingApplication.Models;
using System.Text;
using System.Text.Json;

namespace SchedulingApplication.Services
{
    public class DingTalkService : IDingTalkService
    {
        private readonly ILogger<DingTalkService> _logger;
        private readonly DingTalkSettings _settings;
        private string? _accessToken;
        private DateTime _accessTokenExpiry = DateTime.MinValue;
        private readonly HttpClient _httpClient;

        public DingTalkService(
            ILogger<DingTalkService> logger,
            IOptions<DingTalkSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _httpClient = new HttpClient();
        }

        private async Task GetAccessTokenAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dingtalk.com/v1.0/oauth2/userAccessToken");
                var content = new
                {
                    clientId = _settings.AppSecret,
                    clientSecret = _settings.RobotCode,
                    code = _settings.CoffeeUserId
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(content),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                _accessToken = tokenResponse.GetProperty("accessToken").GetString();
                var expiresIn = tokenResponse.GetProperty("expireIn").GetInt32();
                _accessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 300); // 提前5分钟过期

                _logger.LogInformation("成功获取钉钉访问令牌");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取钉钉访问令牌失败");
                throw;
            }
        }

        private async Task<string> GetUserIdByMobileAsync(string phoneNumber)
        {
            try
            {
                if (DateTime.UtcNow > _accessTokenExpiry || string.IsNullOrEmpty(_accessToken))
                {
                    await GetAccessTokenAsync();
                }

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://oapi.dingtalk.com/topapi/v2/user/getbymobile?access_token={_accessToken}");

                var content = new { mobile = phoneNumber };
                request.Content = new StringContent(
                    JsonSerializer.Serialize(content),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.GetProperty("errcode").GetInt32() == 0)
                {
                    return result.GetProperty("result").GetProperty("userid").GetString() ?? string.Empty;
                }

                throw new Exception($"获取用户ID失败: {result.GetProperty("errmsg").GetString()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据手机号获取用户ID失败");
                throw;
            }
        }

        public async Task SendNotificationAsync(string phoneNumber, string message)
        {
            try
            {
                if (DateTime.UtcNow > _accessTokenExpiry || string.IsNullOrEmpty(_accessToken))
                {
                    await GetAccessTokenAsync();
                }

                var userId = await GetUserIdByMobileAsync(phoneNumber);
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception($"未找到手机号 {phoneNumber} 对应的钉钉用户");
                }

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.dingtalk.com/v1.0/robot/oToMessages/batchSend");

                request.Headers.Add("x-acs-dingtalk-access-token", _accessToken);

                var content = new
                {
                    robotCode = _settings.RobotCode,
                    userIds = new[] { userId },
                    msgKey = "sampleText",
                    msgParam = JsonSerializer.Serialize(new { content = message })
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(content),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.GetProperty("code").GetString() != "0")
                {
                    throw new Exception($"发送消息失败: {result.GetProperty("message").GetString()}");
                }

                _logger.LogInformation($"成功发送钉钉通知到 {phoneNumber}: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送钉钉通知失败");
                throw;
            }
        }
    }
} 