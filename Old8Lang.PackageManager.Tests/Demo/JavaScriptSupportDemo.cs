using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Old8Lang.PackageManager.Server.Services;
using Old8Lang.PackageManager.Server.Models;
using System.Text;
using System.Text.Json;

namespace Old8Lang.PackageManager.Tests.Demo;

/// <summary>
/// JavaScript/TypeScript 支持功能演示
/// </summary>
public class JavaScriptSupportDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("🚀 JavaScript/TypeScript 支持功能演示\n");

        // 创建服务
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IJavaScriptPackageParser, JavaScriptPackageParser>();

        var serviceProvider = services.BuildServiceProvider();
        var parser = serviceProvider.GetRequiredService<IJavaScriptPackageParser>();
        var logger = serviceProvider.GetRequiredService<ILogger<JavaScriptSupportDemo>>();

        // 1. 测试语言检测
        Console.WriteLine("📋 1. 语言检测测试");
        var testFiles = new[]
        {
            "package.tgz",
            "script.js", 
            "module.ts",
            "types.d.ts",
            "package.json",
            "unknown.txt"
        };

        foreach (var file in testFiles)
        {
            var language = parser.GetLanguageFromExtension(file);
            Console.WriteLine($"   {file} -> {language}");
        }

        // 2. 测试 package.json 解析
        Console.WriteLine("\n📦 2. package.json 解析测试");
        var packageJsonContent = @"{
            ""name"": ""@old8lang/example-package"",
            ""version"": ""1.0.0"",
            ""description"": ""A JavaScript/TypeScript package for Old8Lang"",
            ""main"": ""lib/index.js"",
            ""types"": ""lib/index.d.ts"",
            ""module"": ""lib/index.mjs"",
            ""exports"": {
                ""."": {
                    ""import"": ""./lib/index.mjs"",
                    ""require"": ""./lib/index.js"",
                    ""types"": ""./lib/index.d.ts""
                }
            },
            ""files"": [
                ""lib/"",
                ""types/"",
                ""README.md""
            ],
            ""engines"": {
                ""node"": "">=14.0.0"",
                ""npm"": "">=6.0.0""
            },
            ""dependencies"": {
                ""lodash"": ""^4.17.21"",
                ""express"": ""^4.18.0""
            },
            ""devDependencies"": {
                ""typescript"": ""^5.0.0"",
                ""jest"": ""^29.0.0"",
                ""eslint"": ""^8.0.0""
            },
            ""peerDependencies"": {
                ""react"": "">=16.8.0""
            },
            ""keywords"": [
                ""javascript"",
                ""typescript"",
                ""old8lang"",
                ""utility""
            ]
        }";

        using var packageJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(packageJsonContent));
        var dependencies = await parser.ParsePackageJsonAsync(packageJsonStream);

        Console.WriteLine($"   解析到 {dependencies.Count} 个依赖:");
        foreach (var dep in dependencies)
        {
            var type = dep.IsDevDependency ? "dev" : "prod";
            Console.WriteLine($"   - {dep.PackageName}@{dep.VersionSpec} ({dep.DependencyType}, {type})");
        }

        // 3. 测试包验证
        Console.WriteLine("\n✅ 3. 包验证测试");
        
        // 创建模拟的 gzip 流
        using var gzipStream = new MemoryStream();
        using var gzip = new System.IO.Compression.GZipStream(gzipStream, System.IO.Compression.CompressionMode.Compress, true);
        gzip.Write(new byte[] { 0x50, 0x4B, 0x03, 0x04 }); // ZIP magic bytes
        gzip.Flush();
        gzipStream.Position = 0;

        var isValid = await parser.ValidateJavaScriptPackageAsync(gzipStream);
        Console.WriteLine($"   GZIP 包验证: {(isValid ? "✅ 通过" : "❌ 失败")}");

        // 4. 测试包解析
        Console.WriteLine("\n📂 4. 包解析测试");
        var tarballStream = new MemoryStream(); // 空流用于演示
        var packageInfo = await parser.ParsePackageAsync(tarballStream, "@old8lang/example-package-1.0.0.tgz");
        
        if (packageInfo != null)
        {
            Console.WriteLine($"   包名: {packageInfo.PackageId}");
            Console.WriteLine($"   版本: {packageInfo.Version}");
            Console.WriteLine($"   主入口: {packageInfo.Main}");
            Console.WriteLine($"   类型声明: {packageInfo.Types}");
            Console.WriteLine($"   模块入口: {packageInfo.Module}");
            Console.WriteLine($"   依赖数量: {packageInfo.Dependencies.Count}");
            Console.WriteLine($"   引擎要求: {string.Join(", ", packageInfo.Engines)}");
        }
        else
        {
            Console.WriteLine("   包解析: ⚠️  返回 null（预期，因为是空流）");
        }

        // 5. 测试模型序列化
        Console.WriteLine("\n🔧 5. 数据模型测试");
        var packageModel = new JavaScriptPackageInfo
        {
            PackageId = "@old8lang/demo-package",
            Version = "1.0.0",
            Description = "演示包",
            Author = "Old8Lang Team",
            License = "MIT",
            Main = "lib/index.js",
            Types = "lib/index.d.ts",
            Module = "lib/index.mjs",
            Files = new List<string> { "lib/", "types/", "README.md" },
            Engines = new List<string> { "node@>=14.0.0", "npm@>=6.0.0" },
            Dependencies = new List<ExternalDependencyInfo>
            {
                new() { DependencyType = "npm", PackageName = "lodash", VersionSpec = "^4.17.21", IsDevDependency = false },
                new() { DependencyType = "npm", PackageName = "typescript", VersionSpec = "^5.0.0", IsDevDependency = true }
            }
        };

        var json = JsonSerializer.Serialize(packageModel, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("   JavaScriptPackageInfo JSON 序列化:");
        Console.WriteLine(json);

        Console.WriteLine("\n🎉 JavaScript/TypeScript 支持功能演示完成！");
        Console.WriteLine("\n📝 支持的功能:");
        Console.WriteLine("   ✅ 语言类型检测");
        Console.WriteLine("   ✅ package.json 解析");
        Console.WriteLine("   ✅ 依赖关系提取");
        Console.WriteLine("   ✅ 包格式验证");
        Console.WriteLine("   ✅ NPM tarball 解析");
        Console.WriteLine("   ✅ TypeScript 支持");
        Console.WriteLine("   ✅ 作用域包处理");
    }
}