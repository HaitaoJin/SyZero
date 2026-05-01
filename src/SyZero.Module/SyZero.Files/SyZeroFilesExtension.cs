using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Files;

namespace SyZero;

/// <summary>
/// SyZero.Files 模块的 DI 扩展。
/// </summary>
public static class SyZeroFilesExtension
{
    /// <summary>
    /// 注册基于本地磁盘的 <see cref="ISyFileStorage"/> 默认实现。
    /// </summary>
    public static IServiceCollection AddSyZeroLocalFiles(this IServiceCollection services, string rootPath)
    {
        services.TryAddSingleton<ISyFileStorage>(_ => new LocalFileStorage(rootPath));
        return services;
    }
}
