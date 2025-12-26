using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Old8Lang.PackageManager.Server.Middleware;

/// <summary>
/// 语言检测中间件
/// 从请求头或查询参数中获取语言设置
/// </summary>
public class LanguageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _supportedCultures = { "en", "zh-CN" };

    public LanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var culture = GetCultureFromRequest(context);

        if (!string.IsNullOrEmpty(culture) && _supportedCultures.Contains(culture))
        {
            var cultureInfo = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
        }

        await _next(context);
    }

    private string? GetCultureFromRequest(HttpContext context)
    {
        // 1. 从查询参数获取
        if (context.Request.Query.TryGetValue("lang", out var langQuery))
        {
            return langQuery.FirstOrDefault();
        }

        // 2. 从 Accept-Language 头获取
        var acceptLanguage = context.Request.Headers.AcceptLanguage.FirstOrDefault();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var languages = acceptLanguage.Split(',')
                .Select(l => l.Split(';')[0].Trim())
                .ToArray();

            foreach (var lang in languages)
            {
                if (_supportedCultures.Contains(lang))
                {
                    return lang;
                }
            }

            // 尝试匹配主语言（如 zh 匹配 zh-CN）
            foreach (var lang in languages)
            {
                var mainLang = lang.Split('-')[0];
                var match = _supportedCultures.FirstOrDefault(sc => sc.StartsWith(mainLang));
                if (match != null)
                {
                    return match;
                }
            }
        }

        return null;
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class LanguageMiddlewareExtensions
{
    public static IApplicationBuilder UseLanguageMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LanguageMiddleware>();
    }
}
