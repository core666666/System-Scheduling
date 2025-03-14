using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SchedulingApplication.Data;
using SchedulingApplication.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.JSInterop;
using SchedulingApplication.Models;

var builder = WebApplication.CreateBuilder(args);

// 设置开发环境
builder.Environment.EnvironmentName = "Development";

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 添加数据库服务
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加认证服务
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "SchedulingApp.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        
        // 添加Cookie验证事件
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/login"))
                {
                    return Task.CompletedTask;
                }
                if (context.Request.Path.StartsWithSegments("/_blazor"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            // 添加验证事件
            OnValidatePrincipal = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("验证认证Cookie: User={User}, Path={Path}",
                    context.Principal?.Identity?.Name,
                    context.Request.Path);
            }
        };
    });
builder.Services.AddAuthorization();

// 添加HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 添加应用服务 - 先注册 AuthService
builder.Services.AddScoped<AuthService>();

// 再添加认证状态提供程序 - 确保 AuthService 已经注册
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// 注册JSInterop服务
builder.Services.AddScoped<AppJsInterop>();

// 添加钉钉服务
builder.Services.Configure<DingTalkSettings>(
    builder.Configuration.GetSection("DingTalk"));
builder.Services.AddScoped<IDingTalkService, DingTalkService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 调整中间件顺序 - 静态文件应该在最前面
app.UseStaticFiles();
app.UseRouting();

// 认证相关中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub(options =>
{
    options.CloseOnAuthenticationExpiration = true;
});
app.MapFallbackToPage("/_Host");

// 确保数据库已创建并应用迁移
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try 
    {
        // 检查数据库是否存在，如果不存在则创建它
        bool databaseExists = db.Database.CanConnect();
        
        if (!databaseExists)
        {
            logger.LogInformation("数据库不存在，正在创建...");
            db.Database.EnsureCreated();
            logger.LogInformation("数据库已成功创建！");
        }
        else
        {
            // 数据库存在，但可能缺少某些表，尝试确保表结构完整
            try
            {
                // 尝试查询每个表以检查它们是否存在
                var userCount = db.Users.Count();
                var staffCount = db.Staff.Count();
                var scheduleCount = db.Schedules.Count();
                var configCount = db.NotificationConfigs.Count();
                
                logger.LogInformation("数据库结构检查完成，所有表都存在");
            }
            catch (Exception ex) when (ex.Message.Contains("no such table"))
            {
                logger.LogWarning($"检测到数据库缺少表: {ex.Message}");
                
                // 不删除数据库，而是尝试安全地添加缺失的表
                try
                {
                    // 关闭当前连接
                    db.Database.CloseConnection();
                    
                    // 获取SQLite连接
                    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                    var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                    connection.Open();
                    
                    // 检查并创建缺失的表
                    // 用户表
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Users (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Username TEXT NOT NULL,
                                Password TEXT NOT NULL,
                                LastLoginTime TEXT NULL,
                                IsAdmin INTEGER NOT NULL DEFAULT 0,
                                IsActive INTEGER NOT NULL DEFAULT 1,
                                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                            );
                            
                            -- 检查是否存在默认管理员账户
                            INSERT INTO Users (Id, Username, Password, IsAdmin, IsActive, CreatedAt)
                            SELECT 1, 'admin', 'admin123', 1, 1, datetime('now')
                            WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Id = 1);
                            
                            -- 检查是否需要为现有用户添加新列
                            PRAGMA table_info(Users);";
                        
                        using (var reader = command.ExecuteReader())
                        {
                            bool hasCreatedAt = false;
                            bool hasIsActive = false;
                            
                            // 检查Users表是否已经有CreatedAt和IsActive列
                            while (reader.Read())
                            {
                                string columnName = reader.GetString(1); // 列名是第二列
                                if (columnName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasCreatedAt = true;
                                }
                                else if (columnName.Equals("IsActive", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasIsActive = true;
                                }
                            }
                            
                            // 需要关闭当前读取器
                            reader.Close();
                            
                            // 如果没有IsActive列，添加它
                            if (!hasIsActive)
                            {
                                logger.LogInformation("Users表中缺少IsActive列，正在添加...");
                                try
                                {
                                    // 执行ALTER TABLE添加列
                                    command.CommandText = @"
                                        ALTER TABLE Users 
                                        ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;";
                                    command.ExecuteNonQuery();
                                    
                                    // 更新现有记录
                                    command.CommandText = @"
                                        UPDATE Users 
                                        SET IsActive = 1
                                        WHERE IsActive IS NULL;";
                                    command.ExecuteNonQuery();
                                    
                                    logger.LogInformation("成功添加IsActive列并更新现有记录");
                                }
                                catch (Exception exCol)
                                {
                                    logger.LogError(exCol, "添加IsActive列时出错");
                                }
                            }
                        }
                    }
                    
                    // 员工表
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Staff (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Name TEXT NOT NULL,
                                PhoneNumber TEXT NOT NULL,
                                IsActive INTEGER NOT NULL DEFAULT 1,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NULL
                            );
                            
                            -- 添加示例员工数据（如果表是新创建的）
                            INSERT INTO Staff (Id, Name, PhoneNumber, IsActive, CreatedAt)
                            SELECT 1, '张三', '13800000001', 1, datetime('now')
                            WHERE NOT EXISTS (SELECT 1 FROM Staff WHERE Id = 1);
                            
                            INSERT INTO Staff (Id, Name, PhoneNumber, IsActive, CreatedAt)
                            SELECT 2, '李四', '13800000002', 1, datetime('now')
                            WHERE NOT EXISTS (SELECT 1 FROM Staff WHERE Id = 2);";
                        command.ExecuteNonQuery();
                    }
                    
                    // 排班表
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Schedules (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Date TEXT NOT NULL,
                                StaffId INTEGER NULL,
                                NotificationSent INTEGER NOT NULL DEFAULT 0,
                                NotificationSentTime TEXT NULL,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NULL,
                                FOREIGN KEY (StaffId) REFERENCES Staff (Id)
                            );";
                        command.ExecuteNonQuery();
                        
                        // 添加今天和未来几天的示例排班数据
                        var today = DateTime.Today;
                        for (int i = 0; i < 7; i++)
                        {
                            var date = today.AddDays(i);
                            var staffId = (i % 2) + 1; // 轮流安排员工1和2
                            var dateString = date.ToString("yyyy-MM-dd");
                            
                            // 检查该日期是否已有排班
                            command.CommandText = $"SELECT COUNT(*) FROM Schedules WHERE Date = '{dateString}'";
                            var count = Convert.ToInt32(command.ExecuteScalar());
                            
                            if (count == 0)
                            {
                                // 添加排班记录
                                command.CommandText = $@"
                                    INSERT INTO Schedules (Date, StaffId, NotificationSent, CreatedAt)
                                    VALUES ('{dateString}', {staffId}, {(i == 0 ? 1 : 0)}, datetime('now'))";
                                command.ExecuteNonQuery();
                                
                                logger.LogInformation($"已为 {dateString} 创建示例排班数据");
                            }
                        }
                    }
                    
                    // 通知配置表
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS NotificationConfigs (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                NotificationHour INTEGER NOT NULL,
                                NotificationMinute INTEGER NOT NULL,
                                IsEnabled INTEGER NOT NULL DEFAULT 1,
                                AdvanceDays INTEGER NOT NULL DEFAULT 1,
                                NotificationTemplate TEXT NOT NULL,
                                LastUpdated TEXT NOT NULL
                            );
                            
                            -- 检查是否存在默认配置
                            INSERT INTO NotificationConfigs (Id, NotificationHour, NotificationMinute, IsEnabled, AdvanceDays, NotificationTemplate, LastUpdated)
                            SELECT 1, 17, 0, 1, 1, '尊敬的{Name}，提醒您明天({Date} {DayOfWeek})将由您值班，请做好准备。', datetime('now')
                            WHERE NOT EXISTS (SELECT 1 FROM NotificationConfigs WHERE Id = 1);";
                        command.ExecuteNonQuery();
                    }
                    
                    // 关闭连接
                    connection.Close();
                    
                    // 重新连接EF核心上下文
                    db.Database.OpenConnection();
                    
                    logger.LogInformation("成功创建缺失的表，数据库结构已更新");
                }
                catch (Exception updateEx)
                {
                    logger.LogError($"更新数据库架构时出错: {updateEx.Message}");
                    logger.LogError("应用程序可能无法正常工作。请考虑手动修复数据库或联系管理员。");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError($"初始化数据库时出错: {ex.Message}");
        logger.LogError("应用程序可能无法正常工作。请检查数据库连接配置。");
    }
}

app.Run();
