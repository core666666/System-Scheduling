using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dingtalk.com/v1.0/oauth2/accessToken");

                var content = new StringContent(
                JsonConvert.SerializeObject(new { appKey = _settings.RobotCode, appSecret = _settings.AppSecret }),
                Encoding.UTF8,
                "application/json");

                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AccessTokenResponse>(responseContent);

                _accessToken = result.AccessToken;
                _accessTokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpireIn);

                //return _accessToken;

                _logger.LogInformation("成功获取钉钉访问令牌");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取钉钉访问令牌失败");
                throw;
            }
        }

        public class AccessTokenResponse
        {
            [JsonProperty("accessToken")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonProperty("expireIn")]
            public int ExpireIn { get; set; } = int.MaxValue;
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
                    JsonConvert.SerializeObject(content),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                
                var result = JsonConvert.DeserializeObject<JObject>(responseContent);

                if (result["errcode"].Value<int>() == 0)
                {
                    return result["result"]["userid"].Value<string>() ?? string.Empty;
                }

                throw new Exception($"获取用户ID失败: {result["errmsg"].Value<string>()}");
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
                    msgParam = JsonConvert.SerializeObject(new { content = message })
                };

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(content),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                
                var result = JsonConvert.DeserializeObject<JObject>(responseContent);

                if (result["processQueryKey"] == null || string.IsNullOrEmpty(result["processQueryKey"].Value<string>()))
                {
                    string errorMessage = result["errmsg"]?.Value<string>() ?? "未知错误";
                    throw new Exception($"发送消息失败: {errorMessage}");
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