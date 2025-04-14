using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchedulingApplication.Data;
using SchedulingApplication.Models;

namespace SchedulingApplication.Services
{
    public class NotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationService> _logger;
        private Timer? _timer;

        public NotificationService(
            IServiceProvider serviceProvider, 
            ILogger<NotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("通知服务已启动，正在监控通知任务...");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation("通知服务正在检查是否需要发送通知...");
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dingTalkService = scope.ServiceProvider.GetRequiredService<IDingTalkService>();
                
                try
                {
                    // 获取通知配置
                    var config = await dbContext.NotificationConfigs.FirstOrDefaultAsync();
                    if (config == null || !config.IsEnabled)
                    {
                        _logger.LogInformation("通知功能未启用或配置不存在");
                        return;
                    }
                    
                    // 检查是否到了通知时间
                    var now = DateTime.Now;
                    if (now.Hour == config.NotificationHour && now.Minute == config.NotificationMinute)
                    {
                        _logger.LogInformation("当前时间匹配通知时间，准备发送通知");
                        
                        // 计算目标日期（值班日期）
                        var targetDate = DateTime.Today.AddDays(config.AdvanceDays);
                        
                        // 查询目标日期的值班人员
                        var schedule = await dbContext.Schedules
                            .Include(s => s.Staff)
                            .FirstOrDefaultAsync(s => s.Date.Date == targetDate.Date);
                        
                        if (schedule != null && schedule.Staff != null)
                        {
                            // 构建通知内容
                            var dayOfWeek = GetChineseDayOfWeek(targetDate.DayOfWeek);
                            var content = config.NotificationTemplate
                                .Replace("{Name}", schedule.Staff.Name)
                                .Replace("{Date}", targetDate.ToString("yyyy-MM-dd"))
                                .Replace("{DayOfWeek}", dayOfWeek);
                            
                            // 发送通知
                            bool isSuccess = true;
                            string errorMessage = "";
                            
                            try 
                            {
                                await dingTalkService.SendNotificationAsync(
                                    schedule.Staff.PhoneNumber, 
                                    content
                                );
                                _logger.LogInformation(
                                    "已成功发送通知到 {StaffName} ({Phone})", 
                                    schedule.Staff.Name, 
                                    schedule.Staff.PhoneNumber
                                );
                                
                                // 更新排班记录的通知状态
                                schedule.NotificationSent = true;
                                schedule.NotificationSentTime = DateTime.Now;
                                dbContext.Schedules.Update(schedule);
                            }
                            catch (Exception ex)
                            {
                                isSuccess = false;
                                errorMessage = ex.Message;
                                _logger.LogError(
                                    ex, 
                                    "发送通知到 {StaffName} ({Phone}) 失败", 
                                    schedule.Staff.Name, 
                                    schedule.Staff.PhoneNumber
                                );
                            }
                            
                            // 记录通知日志
                            var notificationLog = new NotificationLog
                            {
                                SentTime = DateTime.Now,
                                RecipientName = schedule.Staff.Name,
                                RecipientPhone = schedule.Staff.PhoneNumber,
                                Content = content,
                                Type = NotificationType.Scheduled,
                                IsSuccess = isSuccess,
                                ErrorMessage = errorMessage
                            };
                            
                            dbContext.NotificationLogs.Add(notificationLog);
                            await dbContext.SaveChangesAsync();
                        }
                        else
                        {
                            _logger.LogWarning("未找到日期 {TargetDate} 的值班安排", targetDate.ToString("yyyy-MM-dd"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "通知服务执行过程中发生错误");
                }
            }
        }

        private string GetChineseDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "星期一";
                case DayOfWeek.Tuesday: return "星期二";
                case DayOfWeek.Wednesday: return "星期三";
                case DayOfWeek.Thursday: return "星期四";
                case DayOfWeek.Friday: return "星期五";
                case DayOfWeek.Saturday: return "星期六";
                case DayOfWeek.Sunday: return "星期日";
                default: return "";
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("通知服务正在停止...");
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
} 