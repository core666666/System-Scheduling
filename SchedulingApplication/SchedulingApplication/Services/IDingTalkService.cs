namespace SchedulingApplication.Services
{
    public interface IDingTalkService
    {
        Task SendNotificationAsync(string phoneNumber, string message);
        
        /// <summary>
        /// 通过手机号获取钉钉用户ID
        /// </summary>
        /// <param name="phoneNumber">员工手机号</param>
        /// <returns>钉钉用户ID</returns>
        Task<string> GetUserIdByMobileAsync(string phoneNumber);
    }
} 