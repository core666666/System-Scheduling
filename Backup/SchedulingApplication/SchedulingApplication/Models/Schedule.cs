using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulingApplication.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public int? StaffId { get; set; }

        public Staff? Staff { get; set; }

        // 通知状态
        public bool NotificationSent { get; set; } = false;
        public DateTime? NotificationSentTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // 以下是不映射到数据库的辅助属性
        [NotMapped]
        public string DayOfWeek => Date.ToString("dddd");

        [NotMapped]
        public bool IsPast => Date.Date < DateTime.Today;

        [NotMapped]
        public bool IsToday => Date.Date == DateTime.Today;

        [NotMapped]
        public bool IsFuture => Date.Date > DateTime.Today;
    }
} 