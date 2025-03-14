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
        [StringLength(500, ErrorMessage = "通知模板长度不能超过500个字符")]
        public string NotificationTemplate { get; set; } = "尊敬的{Name}，提醒您明天({Date} {DayOfWeek})将由您值班，请做好准备。";

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [NotMapped]
        public string NotificationTime => $"{NotificationHour:D2}:{NotificationMinute:D2}";
    }
} 