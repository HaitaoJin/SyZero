using Microsoft.Extensions.DependencyInjection;
using System;

namespace SyZero.Sdk;

/// <summary>
/// SyZero.Sdk 模块的 DI 扩展。
/// </summary>
public static class SyZeroSdkExtension
{
    /// <summary>
    /// 注册一个 SyServiceClient 派生 typed client，并指定基地址。
    /// </summary>
    public static IHttpClientBuilder AddSyServiceClient<TClient>(
        this IServiceCollection services,
        string baseAddress)
        where TClient : SyServiceClient
    {
        if (string.IsNullOrWhiteSpace(baseAddress)) throw new ArgumentException("baseAddress is required", nameof(baseAddress));

        return services.AddHttpClient<TClient>(c => c.BaseAddress = new Uri(baseAddress));
    }
}
