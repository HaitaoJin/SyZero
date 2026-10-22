using System.Threading;
using System.Threading.Tasks;

namespace SyZero.BackgroundJobs
{
    /// <summary>
    /// 后台任务契约。每种任务一个实现，由 DI 容器解析，DI 生命周期 = Scoped（每次执行新建作用域）。
    /// </summary>
    public interface IBackgroundJob<in TArgs>
    {
        Task ExecuteAsync(TArgs args, CancellationToken cancellationToken = default);
    }
}
