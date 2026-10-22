using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.DataProtection;

namespace SyZero
{
    /// <summary>
    /// SyZero.DataProtection 扩展方法。
    /// </summary>
    public static class SyZeroDataProtectionExtension
    {
        /// <summary>
        /// 注册 SyZero 字段级加密能力。
        /// 默认会调用 ASP.NET Core 的 <see cref="DataProtectionServiceCollectionExtensions.AddDataProtection(IServiceCollection)"/>，
        /// 业务可在调用本方法前后追加 <c>.PersistKeysToFileSystem()</c> 等密钥持久化配置。
        /// </summary>
        public static IServiceCollection AddSyZeroDataProtection(
            this IServiceCollection services,
            Action<IDataProtectionBuilder>? configure = null)
        {
            var builder = services.AddDataProtection();
            configure?.Invoke(builder);

            services.TryAddSingleton<IFieldEncryptor, DataProtectorFieldEncryptor>();
            return services;
        }
    }
}
