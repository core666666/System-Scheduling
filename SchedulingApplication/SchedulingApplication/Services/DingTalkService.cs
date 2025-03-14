namespace SchedulingApplication.Services
{
    public class DingTalkService : IDingTalkService
    {
        private readonly ILogger<DingTalkService> _logger;

        public DingTalkService(ILogger<DingTalkService> logger)
        {
            _logger = logger;
        }

        public async Task SendNotificationAsync(string phoneNumber, string message)
        {
            // 这里是钉钉通知的空实现
            _logger.LogInformation($"模拟发送钉钉通知到 {phoneNumber}: {message}");
            await Task.CompletedTask;
        }
    }
} 