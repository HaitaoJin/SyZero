using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 把当前租户映射到「数据连接」的解析器。默认实现返回 <see cref="TenantInfo.ConnectionString"/>，
    /// 业务侧可替换为按 Id 路由到不同连接池 / Sharding 中心。
    /// </summary>
    public interface ITenantConnectionResolver
    {
        Task<string?> ResolveAsync(TenantInfo? tenant, CancellationToken cancellationToken = default);
    }

    /// <summary>默认实现：直接读 <see cref="TenantInfo.ConnectionString"/>，null 表示走宿主默认连接。</summary>
    public class DefaultTenantConnectionResolver : ITenantConnectionResolver, ISingletonDependency
    {
        public Task<string?> ResolveAsync(TenantInfo? tenant, CancellationToken cancellationToken = default)
            => Task.FromResult(tenant?.ConnectionString);
    }
}
