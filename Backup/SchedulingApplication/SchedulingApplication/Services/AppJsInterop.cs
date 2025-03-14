using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace SchedulingApplication.Services
{
    public class AppJsInterop
    {
        private readonly ILogger<AppJsInterop> _logger;
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AppJsInterop(
            ILogger<AppJsInterop> logger, 
            NavigationManager navigationManager,
            AuthenticationStateProvider authStateProvider)
        {
            _logger = logger;
            _navigationManager = navigationManager;
            _authStateProvider = authStateProvider;
        }

        [JSInvokable("ForceStateUpdate")]
        public async Task ForceStateUpdate()
        {
            try
            {
                _logger.LogInformation("JS调用ForceStateUpdate，当前URL: {Url}", _navigationManager.Uri);
                
                // 如果认证状态提供程序是我们自定义的类型，通知状态变化
                if (_authStateProvider is CustomAuthStateProvider customProvider)
                {
                    var authState = await _authStateProvider.GetAuthenticationStateAsync();
                    if (authState.User?.Identity?.IsAuthenticated == true)
                    {
                        _logger.LogInformation("通知认证状态更新: {User}", authState.User.Identity.Name);
                        customProvider.NotifyAuthenticationStateChanged(authState.User);
                    }
                    else
                    {
                        _logger.LogWarning("无法进行状态更新：用户未认证");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从JS调用ForceStateUpdate时发生错误");
            }
        }
    }
} 