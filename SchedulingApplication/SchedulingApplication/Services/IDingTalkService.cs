namespace SchedulingApplication.Services
{
    public interface IDingTalkService
    {
        Task SendNotificationAsync(string phoneNumber, string message);
    }
} 