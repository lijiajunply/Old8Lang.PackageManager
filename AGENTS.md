# Old8Lang Package Manager - Agent Guidelines

**请使用中文回答问题**

## 构建/格式化/测试命令

```bash
# 构建解决方案
dotnet build Old8Lang.PackageManager.sln

# 运行应用程序
dotnet run --project Old8Lang.PackageManager

# 测试特定项目（如果测试存在）
dotnet test Old8Lang.PackageManager.Core.Tests --filter "TestName"

# 格式化代码
dotnet format Old8Lang.PackageManager.sln

# 清理构建产物
dotnet clean Old8Lang.PackageManager.sln
```

## 代码风格指南

### 导入与命名空间
- 使用 `ImplicitUsings=enable`（已在项目文件中启用）
- 按顺序分组导入：System 命名空间、第三方库、本地命名空间
- 谨慎使用 `using static` 仅用于常用静态成员

### 格式化与类型
- 遵循 C# 约定（公共成员使用 PascalCase，私有成员使用 camelCase）
- 启用可空引用类型（`Nullable=enable`）
- 类型明显时使用 `var` 声明局部变量
- 单行实现优先使用表达式体方法
- 公共 API 使用 XML 文档（`/// <summary>`）

### 命名约定
- 类：PascalCase（如 `DefaultPackageInstaller`）
- 接口：PascalCase 带 `I` 前缀（如 `IPackageInstaller`）
- 方法：PascalCase 带 async 后缀（如 `InstallPackageAsync`）
- 字段：camelCase 带 `_` 前缀的私有字段（如 `_sourceManager`）
- 常量：PascalCase
- 属性：PascalCase

### 错误处理
- 使用具体异常类型的 try-catch 块
- 将警告记录到集合属性（`result.Warnings.Add()`）
- 返回包含 Success/Message 属性的结果对象
- 避免为预期错误条件抛出异常
- 所有 I/O 操作使用 async/await

### 架构模式
- 通过构造函数进行依赖注入
- 接口隔离原则（关注点分离）
- 服务应尽可能无状态
- 异步操作使用 `Task<T>`
- 遵循数据访问的仓储模式