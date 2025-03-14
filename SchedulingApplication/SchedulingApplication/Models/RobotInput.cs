namespace SchedulingApplication.Models
{
    public class RobotInput
    {
        public string robotCode { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string[] userIds { get; set; } = Array.Empty<string>();
        public string msgKey { get; set; } = string.Empty;
        public string msgParam { get; set; } = string.Empty;
    }
} 