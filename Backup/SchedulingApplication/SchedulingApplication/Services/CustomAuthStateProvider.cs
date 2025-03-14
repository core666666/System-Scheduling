using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace SchedulingApplication.Services
{
    public class CustomAuthStateProvider : ServerAuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CustomAuthStateProvider> _logger;
        private readonly AuthService _authService;
        private ClaimsPrincipal _cachedUser;

        public CustomAuthStateProvider(
            IHttpContextAccessor httpContextAccessor,
            AuthService authService,
            ILogger<CustomAuthStateProvider> logger)
        {
            _logger = logger;
            _logger.LogInformation("创建 CustomAuthStateProvider 实例...");
            
            _httpContextAccessor = httpContextAccessor ?? 
                throw new ArgumentNullException(nameof(httpContextAccessor));
            
            _authService = authService ?? 
                throw new ArgumentNullException(nameof(authService));
            
            _logger.LogInformation("CustomAuthStateProvider 实例创建成功");
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                _logger.LogInformation("获取认证状态...");
                
                var httpContext = _httpContextAccessor.HttpContext;
                var requestPath = httpContext?.Request.Path ?? new PathString("unknown");
                
                // 先检查缓存的用户
                if (_cachedUser?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("从缓存返回已认证用户: {Username}, 请求路径: {Path}", 
                        _cachedUser.Identity.Name, requestPath);
                    return Task.FromResult(new AuthenticationState(_cachedUser));
                }
                
                // 尝试从当前 HttpContext 获取
                var user = httpContext?.User;
                
                // 如果当前 HttpContext 有认证用户，则缓存并返回
                if (user?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("从 HttpContext 获取并缓存已认证用户: {Username}, 请求路径: {Path}", 
                        user.Identity.Name, requestPath);
                    _cachedUser = user;
                    return Task.FromResult(new AuthenticationState(user));
                }
                
                // 尝试从 AuthService 获取缓存的用户
                user = _authService.GetCurrentUser();
                
                if (user?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("从 AuthService 获取并缓存已认证用户: {Username}, 请求路径: {Path}", 
                        user.Identity.Name, requestPath);
                    _cachedUser = user;
                    return Task.FromResult(new AuthenticationState(user));
                }

                _logger.LogWarning("未找到认证用户, 请求路径: {Path}", requestPath);
                
                // 如果没有认证用户，返回空认证状态
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证状态时发生错误");
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
        }
        
        // 添加一个方法，在登录成功后主动通知认证状态变化
        public void NotifyAuthenticationStateChanged(ClaimsPrincipal user)
        {
            _logger.LogInformation("更新认证状态: User={User}, IsAuthenticated={IsAuthenticated}", 
                user?.Identity?.Name, user?.Identity?.IsAuthenticated);
            
            // 缓存用户
            _cachedUser = user;
            
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
            _logger.LogInformation("已通知 Blazor 组件认证状态已更改");
        }
    }
} 