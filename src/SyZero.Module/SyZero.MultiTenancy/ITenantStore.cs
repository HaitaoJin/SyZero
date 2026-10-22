using System.Threading;
using System.Threading.Tasks;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 租户存储抽象：业务侧实现把租户信息从数据库 / 缓存 / 远程服务取出。
    /// </summary>
    public interface ITenantStore
    {
        Task<TenantInfo?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<TenantInfo?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
