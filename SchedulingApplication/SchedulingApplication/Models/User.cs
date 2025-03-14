using System.ComponentModel.DataAnnotations;

namespace SchedulingApplication.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? LastLoginTime { get; set; }
        
        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; } = true;
    }
} 