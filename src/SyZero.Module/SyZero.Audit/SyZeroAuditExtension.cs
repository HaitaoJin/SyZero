using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Audit;

namespace SyZero
{
    /// <summary>
    /// SyZero.Audit 扩展方法。
    /// </summary>
    public static class SyZeroAuditExtension
    {
        /// <summary>
        /// 注册 SyZero 审计日志服务（默认使用内存存储 <see cref="MemoryAuditLogStore"/>）。
        /// 如需替换为持久化存储，可在调用本方法后用 <c>services.AddSingleton&lt;IAuditLogStore, MyStore&gt;()</c> 覆盖。
        /// </summary>
        public static IServiceCollection AddSyZeroAudit(this IServiceCollection services)
        {
            services.TryAddSingleton<IAuditLogStore, MemoryAuditLogStore>();
            services.TryAddSingleton<IAuditDataMasker, DefaultAuditDataMasker>();
            return services;
        }
    }
}
