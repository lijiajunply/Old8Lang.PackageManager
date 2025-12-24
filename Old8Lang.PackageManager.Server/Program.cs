using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置选项
builder.Services.Configure<PackageStorageOptions>(builder.Configuration.GetSection("PackageStorage"));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<OidcConfiguration>(builder.Configuration.GetSection("Authentication:OIDC"));
builder.Services.Configure<AuthenticationConfiguration>(builder.Configuration.GetSection("Authentication"));

// 配置数据库
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
switch (dbProvider.ToUpperInvariant())
{
    case "POSTGRESQL":
        // 先添加 Npgsql 包，暂时注释掉
        // builder.Services.AddDbContext<PackageManagerDbContext>(options =>
        //     options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));
        builder.Services.AddDbContext<PackageManagerDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
        break;
    case "SQLSERVER":
        // 先添加 SqlServer 包，暂时注释掉
        // builder.Services.AddDbContext<PackageManagerDbContext>(options =>
        //     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddDbContext<PackageManagerDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
        break;
    default:
        builder.Services.AddDbContext<PackageManagerDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
        break;
}

// 添加用户服务
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<OidcAuthenticationService>();

// 配置认证
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc-github"; // 默认使用 GitHub 登录
});

// 延迟配置认证，在所有服务注册后
builder.Services.AddSingleton<IStartupFilter>(new AuthenticationStartupFilter());

// 添加服务
builder.Services.AddScoped<IPackageStorageService, PackageStorageService>();
builder.Services.AddScoped<IPackageManagementService, PackageManagementService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IPackageSearchService, PackageSearchService>();
builder.Services.AddScoped<IPythonPackageParser, PythonPackageParser>();
builder.Services.AddScoped<IJavaScriptPackageParser, JavaScriptPackageParser>();
builder.Services.AddScoped<IPackageSignatureService, PackageSignatureService>();
builder.Services.AddScoped<IPackageIntegrityService, PackageIntegrityService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<OidcAuthenticationService>();

// 添加控制器
builder.Services.AddControllers();

// 添加 API 文档
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 暂时移除速率限制功能
// builder.Services.AddRateLimiter(options =>
// {
//     options.AddPolicy("ApiPolicy", context =>
//         Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
//             partitionKey: context.Connection.RemoteIpAddress?.ToString(),
//             factory: _ => new Microsoft.AspNetCore.RateLimiting.SlidingWindowRateLimiterOptions
//             {
//                 PermitLimit = 100,
//                 Window = TimeSpan.FromMinutes(1),
//                 SegmentsPerWindow = 6
//             }));
// });

var app = builder.Build();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PackageManagerDbContext>();
    await context.Database.MigrateAsync();
}

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

// app.UseRateLimiter();

// 启用中间件
app.Use(async (context, next) =>
{
    // 添加自定义响应头
    context.Response.Headers["X-Powered-By"] = "Old8Lang Package Manager";
    context.Response.Headers["X-API-Version"] = "3.0.0";
    
    await next();
});

// 配置路由
app.MapControllers();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).WithName("HealthCheck");

// 根路径重定向到 API 文档
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();