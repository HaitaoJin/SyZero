using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SyZero.BackgroundJobs
{
    /// <summary>
    /// 单条已排队任务，包含执行委托与可选的入队上下文。
    /// 委托内部已闭包实参，由 Worker 取出后建立 IServiceScope 解析 Job 并执行。
    /// </summary>
    public sealed class BackgroundJobItem
    {
        public string Id { get; init; } = Guid.NewGuid().ToString("N");
        public required string JobTypeName { get; init; }
        public required Func<IServiceProvider, CancellationToken, Task> Invoke { get; init; }
        public int Attempt { get; set; }
        public DateTime EnqueuedAtUtc { get; init; } = DateTime.UtcNow;
    }

    /// <summary>基于 <see cref="Channel{T}"/> 的内存任务队列（单例）。</summary>
    public class BackgroundJobChannel
    {
        public Channel<BackgroundJobItem> Channel { get; }

        public BackgroundJobChannel(IOptions<BackgroundJobsOptions> options)
        {
            var opt = options.Value;
            if (opt.QueueCapacity <= 0)
            {
                Channel = System.Threading.Channels.Channel.CreateUnbounded<BackgroundJobItem>(
                    new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
            }
            else
            {
                Channel = System.Threading.Channels.Channel.CreateBounded<BackgroundJobItem>(
                    new BoundedChannelOptions(opt.QueueCapacity)
                    {
                        SingleReader = false,
                        SingleWriter = false,
                        FullMode = opt.DropOldestWhenFull ? BoundedChannelFullMode.DropOldest : BoundedChannelFullMode.Wait
                    });
            }
        }
    }
}
