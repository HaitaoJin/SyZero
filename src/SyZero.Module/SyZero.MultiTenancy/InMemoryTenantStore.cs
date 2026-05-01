using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 内存版默认 <see cref="ITenantStore"/>，便于测试 / 演示。生产环境请替换为基于仓储或缓存的实现。
    /// </summary>
    public class InMemoryTenantStore : ITenantStore, ISingletonDependency
    {
        private readonly ConcurrentDictionary<long, TenantInfo> _byId = new();
        private readonly ConcurrentDictionary<string, TenantInfo> _byName = new(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>添加或更新一个租户（用于初始化 / 测试）。</summary>
        public void AddOrUpdate(TenantInfo tenant)
        {
            _byId[tenant.Id] = tenant;
            _byName[tenant.Name] = tenant;
        }

        public Task<TenantInfo?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
            => Task.FromResult(_byId.TryGetValue(id, out var t) ? t : null);

        public Task<TenantInfo?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(_byName.TryGetValue(name, out var t) ? t : null);
    }
}
