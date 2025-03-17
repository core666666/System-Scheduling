using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulingApplication.Models
{
    public class NotificationConfig
    {
        [Key]
        public int Id { get; set; }

        public bool IsEnabled { get; set; } = true;

        [Range(0, 23, ErrorMessage = "时间小时必须在0到23之间")]
        public int NotificationHour { get; set; } = 17;

        [Range(0, 59, ErrorMessage = "时间分钟必须在0到59之间")]
        public int NotificationMinute { get; set; } = 0;

        [Range(1, 30, ErrorMessage = "提前通知天数必须在1到30之间")]
        public int AdvanceDays { get; set; } = 1;

        [Required(ErrorMessage = "通知模板不能为空")]
        [StringLength(1000, ErrorMessage = "通知模板长度不能超过1000个字符")]
        public string NotificationTemplate { get; set; } = "# 📅 值日提醒\n\n**<font color='blue'>值班提醒通知</font>**\n\n---\n\n👋 **尊敬的{Name}**：\n\n📢 **温馨提醒**\n\n请您注意，明天({Date} {DayOfWeek})将由您负责值日工作，请做好相关准备。\n\n---\n\n🔹 **请提前安排好您的工作计划**\n🔹 **值日期间请保持电话畅通**\n🔹 **如有特殊情况请及时告知**\n\n---\n\n> 感谢您的配合与支持！��\n\n> 祝您工作愉快！✨✨✨";

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [NotMapped]
        public string NotificationTime => $"{NotificationHour:D2}:{NotificationMinute:D2}";
    }
} 