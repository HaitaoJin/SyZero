using System.Threading;
using System.Threading.Tasks;

namespace SyZero.BackgroundJobs
{
    /// <summary>后台任务投递入口。Scoped 注入。</summary>
    public interface IBackgroundJobManager
    {
        /// <summary>把一个任务排入队列等待执行。返回是否入队成功（队列已满 / 已停止时返回 false）。</summary>
        Task<bool> EnqueueAsync<TJob, TArgs>(TArgs args, CancellationToken cancellationToken = default)
            where TJob : IBackgroundJob<TArgs>;
    }
}
