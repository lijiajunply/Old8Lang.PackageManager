using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 认证启动过滤器
/// </summary>
public class AuthenticationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var oidcService = app.ApplicationServices.GetRequiredService<OidcAuthenticationService>();
            var authBuilder = app.ApplicationServices.GetRequiredService<AuthenticationBuilder>();
            
            // 在这里配置认证
            oidcService.ConfigureOidcAuthentication(authBuilder);
            
            next(app);
        };
    }
}