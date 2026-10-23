using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Files;
using System;

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

    public static IServiceCollection AddSyZeroAliyunOss(this IServiceCollection services, Action<AliyunOssOptions> configure)
    {
        var options = new AliyunOssOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddSingleton<AliyunOssFileStorage>();
        services.AddSingleton<ISyFileStorage>(sp => sp.GetRequiredService<AliyunOssFileStorage>());
        services.AddSingleton<IFileUrlProvider>(sp => sp.GetRequiredService<AliyunOssFileStorage>());
        return services;
    }
}
