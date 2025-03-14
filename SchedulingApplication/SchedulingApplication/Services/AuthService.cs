using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SchedulingApplication.Data;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SchedulingApplication.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        // 添加静态缓存存储最后一次认证的用户
        private static ClaimsPrincipal _lastAuthenticatedUser = null;

        public AuthService(
            ApplicationDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            
            // 确保数据库已初始化
            EnsureDatabaseInitialized();
        }
        
        // 添加确保数据库初始化的方法
        private void EnsureDatabaseInitialized()
        {
            try
            {
                _logger.LogInformation("正在确保数据库已初始化...");
                
                // 检查数据库连接和用户表
                var adminUser = _context.Users.FirstOrDefault(u => u.Username == "admin");
                if (adminUser == null)
                {
                    _logger.LogWarning("未找到admin用户，尝试重新创建...");
                    
                    // 如果admin用户不存在，则创建一个
                    var user = new Models.User
                    {
                        Username = "admin",
                        Password = "admin123",
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Users.Add(user);
                    _context.SaveChanges();
                    
                    _logger.LogInformation("已成功创建admin用户");
                }
                else if (string.IsNullOrEmpty(adminUser.Password))
                {
                    _logger.LogWarning("admin用户密码为空，正在修复...");
                    
                    // 修复空密码
                    adminUser.Password = "admin123";
                    _context.SaveChanges();
                    
                    _logger.LogInformation("已成功修复admin用户密码");
                }
                
                _logger.LogInformation("数据库初始化检查完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保数据库初始化时发生错误");
            }
        }

        // 获取当前认证的用户
        public ClaimsPrincipal GetCurrentUser()
        {
            _logger.LogInformation("GetCurrentUser - 开始获取当前用户");
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("GetCurrentUser - 从HttpContext获取已认证用户: {Username}", 
                    httpContext.User.Identity.Name);
                
                // 同时更新缓存
                _lastAuthenticatedUser = httpContext.User;
                
                return httpContext.User;
            }
            
            if (_lastAuthenticatedUser?.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("GetCurrentUser - 从缓存获取已认证用户: {Username}", 
                    _lastAuthenticatedUser.Identity.Name);
                return _lastAuthenticatedUser;
            }
            
            _logger.LogWarning("GetCurrentUser - 未找到已认证用户");
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            _logger.LogInformation("开始验证用户：{Username}", username);
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogError("HttpContext为空，无法完成身份验证");
                return false;
            }

            bool isBlazorRequest = httpContext.Request.Path.StartsWithSegments("/_blazor");

            try
            {
                _logger.LogInformation("正在查询用户数据...");
                
                // 确保用户名和密码不为空
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("登录失败：用户名或密码为空");
                    return false;
                }
                
                // 先检查用户是否存在
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                    
                if (user == null)
                {
                    _logger.LogWarning("登录失败：用户不存在 - {Username}", username);
                    return false;
                }
                
                // 检查密码字段是否为空
                if (string.IsNullOrEmpty(user.Password))
                {
                    _logger.LogWarning("登录失败：用户密码字段为空 - {Username}", username);
                    return false;
                }
                
                // 验证密码
                if (user.Password != password)
                {
                    _logger.LogWarning("登录失败：密码错误 - {Username}", username);
                    return false;
                }

                _logger.LogInformation("用户验证成功，正在创建身份认证票据...");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authUser = new ClaimsPrincipal(claimsIdentity);
                
                // 保存到静态缓存，确保即使在新请求中也能获取到用户
                _lastAuthenticatedUser = authUser;

                // 如果是 Blazor SignalR 请求，只验证用户而不设置 Cookie
                if (isBlazorRequest)
                {
                    _logger.LogInformation("Blazor SignalR 请求验证成功，用户已缓存");
                    return true;
                }

                // 检查响应是否已经开始（仅对非 Blazor 请求）
                if (httpContext.Response.HasStarted)
                {
                    _logger.LogError("无法完成身份验证：响应已经开始发送。请求路径：{Path}, 方法：{Method}", 
                        httpContext.Request.Path, httpContext.Request.Method);
                    return false;
                }

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.UtcNow,
                    RedirectUri = "/dashboard"
                };

                try
                {
                    _logger.LogInformation("正在清除现有认证信息...");
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    _logger.LogInformation("正在设置新的认证Cookie...");
                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        authUser,
                        authProperties);

                    // 强制更新当前 HttpContext 的用户
                    httpContext.User = authUser;

                    _logger.LogInformation("用户登录成功：{Username}，Cookie已设置", username);
                    return true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Headers are read-only"))
                {
                    _logger.LogError(ex, "认证失败：响应头已锁定。请求路径：{Path}", httpContext.Request.Path);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "设置认证Cookie时发生错误。请求路径：{Path}", httpContext.Request.Path);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户时发生错误。用户名：{Username}", username);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                // 清除静态缓存
                _lastAuthenticatedUser = null;
                
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _logger.LogInformation("用户已成功注销");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注销过程中发生错误");
            }
        }
    }
} 