using Polly;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using Old8Lang.PackageManager.Core.Exceptions;

namespace Old8Lang.PackageManager.Core.Resilience;

/// <summary>
/// 弹性策略配置 - 重试、超时、熔断
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// HTTP 请求重试策略配置
    /// </summary>
    public class HttpRetryOptions
    {
        /// <summary>
        /// 最大重试次数（默认3次）
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// 基础延迟时间（毫秒，默认200ms）
        /// </summary>
        public int BaseDelayMs { get; set; } = 200;

        /// <summary>
        /// 是否启用指数退避（默认启用）
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// 最大延迟时间（毫秒，默认5秒）
        /// </summary>
        public int MaxDelayMs { get; set; } = 5000;
    }

    /// <summary>
    /// 创建 HTTP 请求重试管道
    /// </summary>
    /// <param name="options">重试选项</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateHttpRetryPipeline(
        HttpRetryOptions? options = null,
        ILogger? logger = null)
    {
        options ??= new HttpRetryOptions();

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = options.UseExponentialBackoff
                    ? DelayBackoffType.Exponential
                    : DelayBackoffType.Linear,
                Delay = TimeSpan.FromMilliseconds(options.BaseDelayMs),
                MaxDelay = TimeSpan.FromMilliseconds(options.MaxDelayMs),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<PackageSourceNetworkException>(ex =>
                        ex.StatusCode is >= 500 or 429), // 5xx 服务器错误或限流
                OnRetry = args =>
                {
                    logger?.LogWarning(
                        "HTTP request retry attempt {AttemptNumber} of {MaxAttempts} due to {ExceptionType}: {Message}",
                        args.AttemptNumber,
                        options.MaxRetryAttempts,
                        args.Outcome.Exception?.GetType().Name,
                        args.Outcome.Exception?.Message);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建包解析重试管道（更宽松的策略）
    /// </summary>
    public static ResiliencePipeline CreatePackageResolutionPipeline(ILogger? logger = null)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2, // 依赖解析重试次数较少
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<PackageSourceNetworkException>()
                    .Handle<HttpRequestException>(),
                OnRetry = args =>
                {
                    logger?.LogDebug(
                        "Package resolution retry attempt {AttemptNumber}, waiting {Delay}ms",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30)) // 单次依赖解析超时30秒
            .Build();
    }

    /// <summary>
    /// 创建文件下载重试管道
    /// </summary>
    public static ResiliencePipeline CreateDownloadPipeline(ILogger? logger = null)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5, // 下载允许更多重试
                Delay = TimeSpan.FromMilliseconds(500),
                MaxDelay = TimeSpan.FromSeconds(10),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<IOException>()
                    .Handle<PackageSourceNetworkException>(ex =>
                        ex.StatusCode != 404), // 404 不重试
                OnRetry = args =>
                {
                    logger?.LogWarning(
                        "Download retry attempt {AttemptNumber} of {MaxAttempts}, waiting {Delay}ms",
                        args.AttemptNumber,
                        5,
                        args.RetryDelay.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
