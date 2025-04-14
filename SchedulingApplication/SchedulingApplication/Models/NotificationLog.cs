using System;
using System.ComponentModel.DataAnnotations;

namespace SchedulingApplication.Models
{
    public class NotificationLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime SentTime { get; set; }
        
        [Required]
        public string RecipientName { get; set; }
        
        [Required]
        public string RecipientPhone { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        [Required]
        public NotificationType Type { get; set; }
        
        public bool IsSuccess { get; set; }
        
        public string? ErrorMessage { get; set; }
    }
    
    public enum NotificationType
    {
        Test,       // 测试消息
        Scheduled   // 定时发送的实际消息
    }
} 