using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Server.Services;

namespace Old8Lang.PackageManager.Server.Middleware;

/// <summary>
/// API 密钥认证中间件
/// </summary>
public class ApiKeyAuthenticationMiddleware(RequestDelegate next, ApiOptions apiOptions)
{
    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // 如果不需要 API 密钥验证，直接继续
        if (!apiOptions.RequireApiKey)
        {
            await next(context);
            return;
        }

        // 跳过不需要认证的路径
        var path = context.Request.Path.Value?.ToLowerInvariant();
        var skipAuthPaths = new[] { "/health", "/swagger", "/v3/index.json", "/api/v1/apikeys/validate" };

        if (skipAuthPaths.Any(p => path?.Contains(p) == true))
        {
            await next(context);
            return;
        }

        // 检查 HTTP 方法（GET 请求通常不需要密钥）
        if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // 获取 API 密钥
        var apiKey = GetApiKeyFromRequest(context);

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "需要 API 密钥",
                errorCode = "API_KEY_REQUIRED",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // 验证 API 密钥
        var keyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
        if (keyEntity == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "无效的 API 密钥",
                errorCode = "INVALID_API_KEY",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // 检查权限
        if (!HasRequiredPermission(context, keyEntity.Scopes))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "权限不足",
                errorCode = "INSUFFICIENT_PERMISSIONS",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // 将 API 密钥信息存储在 HttpContext 中
        context.Items["ApiKey"] = keyEntity;

        await next(context);
    }

    private string? GetApiKeyFromRequest(HttpContext context)
    {
        // 从 Authorization header 获取
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader["Bearer ".Length..];
        }

        // 从查询参数获取
        if (context.Request.Query.TryGetValue("api_key", out var apiKey))
        {
            return apiKey.FirstOrDefault();
        }

        // 从 x-api-key header 获取
        if (context.Request.Headers.TryGetValue("X-API-Key", out var headerKey))
        {
            return headerKey.FirstOrDefault();
        }

        return null;
    }

    private bool HasRequiredPermission(HttpContext context, string scopes)
    {
        var requiredScope = GetRequiredScope(context);
        if (string.IsNullOrEmpty(requiredScope))
        {
            return true;
        }

        var availableScopes = scopes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToHashSet();

        return availableScopes.Contains(requiredScope);
    }

    private string? GetRequiredScope(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        var method = context.Request.Method.ToUpperInvariant();

        return path switch
        {
            var p when p?.Contains("/v3/package") == true && method != "GET" => "package:write",
            var p when p?.Contains("/api/v1/apikeys") == true && method != "GET" => "admin:all",
            _ => "package:read"
        };
    }
}