using System.ComponentModel.DataAnnotations;

namespace SchedulingApplication.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "请输入用户名")]
        public string Username { get; set; }

        [Required(ErrorMessage = "请输入密码")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
} 