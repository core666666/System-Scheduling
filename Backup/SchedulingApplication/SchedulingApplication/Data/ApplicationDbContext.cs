using Microsoft.EntityFrameworkCore;
using SchedulingApplication.Models;

namespace SchedulingApplication.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<NotificationConfig> NotificationConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 添加默认管理员账号
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin123", // 在实际项目中应该使用加密密码
                    CreatedAt = DateTime.Now
                }
            );

            // 添加默认通知配置
            modelBuilder.Entity<NotificationConfig>().HasData(
                new NotificationConfig
                {
                    Id = 1,
                    NotificationHour = 17,
                    NotificationMinute = 0,
                    IsEnabled = true,
                    AdvanceDays = 1,
                    NotificationTemplate = "尊敬的{Name}，提醒您明天({Date} {DayOfWeek})将由您值班，请做好准备。",
                    LastUpdated = DateTime.Now
                }
            );
            
            // 添加示例员工数据
            modelBuilder.Entity<Staff>().HasData(
                new Staff
                {
                    Id = 1,
                    Name = "张三",
                    PhoneNumber = "13800000001",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Staff
                {
                    Id = 2,
                    Name = "李四",
                    PhoneNumber = "13800000002",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            );
            
            // 添加当月示例排班数据
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            
            for (int i = 0; i < 7; i++) // 为当月的前7天创建排班
            {
                var day = thisMonth.AddDays(i);
                if (day <= today.AddDays(7)) // 只为未来7天创建排班
                {
                    modelBuilder.Entity<Schedule>().HasData(
                        new Schedule
                        {
                            Id = i + 1,
                            Date = day,
                            StaffId = (i % 2) + 1, // 轮流分配给Id为1和2的员工
                            NotificationSent = day <= today,
                            NotificationSentTime = day <= today ? day.AddDays(-1).AddHours(17) : null,
                            CreatedAt = DateTime.Now
                        }
                    );
                }
            }
        }
    }
} 