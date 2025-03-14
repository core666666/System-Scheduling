using Microsoft.Extensions.Options;
using SchedulingApplication.Models;

namespace SchedulingApplication.Services
{
    public class DingTalkService : IDingTalkService
    {
        private readonly ILogger<DingTalkService> _logger;
        private readonly DingTalkSettings _settings;

        public DingTalkService(
            ILogger<DingTalkService> logger,
            IOptions<DingTalkSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task SendNotificationAsync(string phoneNumber, string message)
        {
            // 使用配置的AppSecret和RobotCode发送钉钉通知
            _logger.LogInformation($"使用AppSecret: {_settings.AppSecret.Substring(0, 4)}... 发送钉钉通知到 {phoneNumber}: {message}");
            _logger.LogInformation($"机器人代码: {_settings.RobotCode}");
            _logger.LogInformation($"咖啡用户ID: {_settings.CoffeeUserId}");
            await Task.CompletedTask;
        }
    }
} 