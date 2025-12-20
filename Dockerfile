# 使用 .NET 10 运行时作为基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["Old8Lang.PackageManager.Server/Old8Lang.PackageManager.Server.csproj", "Old8Lang.PackageManager.Server/"]
COPY ["Old8Lang.PackageManager.Core/Old8Lang.PackageManager.Core.csproj", "Old8Lang.PackageManager.Core/"]

# 还原依赖
RUN dotnet restore "Old8Lang.PackageManager.Server/Old8Lang.PackageManager.Server.csproj"

# 复制所有源代码
COPY . .

# 构建应用
WORKDIR "/src/Old8Lang.PackageManager.Server"
RUN dotnet build "Old8Lang.PackageManager.Server.csproj" -c Release -o /app/build

# 发布应用
FROM build AS publish
RUN dotnet publish "Old8Lang.PackageManager.Server.csproj" -c Release -o /app/publish

# 运行时阶段
FROM base AS final
WORKDIR /app

# 创建非 root 用户
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# 复制发布文件
COPY --from=publish /app/publish .

# 创建包存储目录
RUN mkdir -p /app/packages

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# 启动应用
ENTRYPOINT ["dotnet", "Old8Lang.PackageManager.Server.dll"]