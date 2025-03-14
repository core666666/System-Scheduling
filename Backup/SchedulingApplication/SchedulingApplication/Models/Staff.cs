using System.ComponentModel.DataAnnotations;

namespace SchedulingApplication.Models
{
    public class Staff
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        public string Name { get; set; }

        [Required(ErrorMessage = "手机号不能为空")]
        [StringLength(15, ErrorMessage = "手机号长度不能超过15个字符")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "请输入有效的手机号码")]
        public string PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
} 