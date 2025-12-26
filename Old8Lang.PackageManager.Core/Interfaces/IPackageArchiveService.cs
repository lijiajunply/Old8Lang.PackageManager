using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包打包服务接口
/// </summary>
public interface IPackageArchiveService
{
    /// <summary>
    /// 将包文件夹打包成 .o8pkg 压缩包
    /// </summary>
    /// <param name="sourcePath">包文件夹路径</param>
    /// <param name="outputPath">输出的 .o8pkg 文件路径（可选，不指定则在源路径旁边生成）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的包文件路径</returns>
    Task<string> PackAsync(string sourcePath, string? outputPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解包 .o8pkg 文件到指定文件夹
    /// </summary>
    /// <param name="packagePath">.o8pkg 文件路径</param>
    /// <param name="destinationPath">解包目标文件夹路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UnpackAsync(string packagePath, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证包文件夹结构是否有效
    /// </summary>
    /// <param name="sourcePath">包文件夹路径</param>
    /// <returns>验证结果（是否有效，错误消息）</returns>
    Task<(bool IsValid, string Message)> ValidatePackageStructureAsync(string sourcePath);

    /// <summary>
    /// 从包文件夹中读取包元数据
    /// </summary>
    /// <param name="sourcePath">包文件夹路径</param>
    /// <returns>包元数据</returns>
    Task<Package?> ReadPackageMetadataAsync(string sourcePath);
}
