using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 租户解析器。多个解析器按顺序尝试，第一个返回非空值的胜出。
    /// </summary>
    public interface ITenantResolver
    {
        /// <summary>解析当前请求所属租户名/Id（解析失败返回 null 让后续解析器尝试）。</summary>
        Task<TenantResolveResult?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>解析结果。Name / Id 至少一个非空。</summary>
    public class TenantResolveResult
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
    }
}
