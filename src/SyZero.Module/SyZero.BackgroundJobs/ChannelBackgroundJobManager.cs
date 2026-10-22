using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyZero.Dependency;

namespace SyZero.BackgroundJobs
{
    /// <summary>基于 <see cref="BackgroundJobChannel"/> 的默认 <see cref="IBackgroundJobManager"/>。</summary>
    public class ChannelBackgroundJobManager : IBackgroundJobManager, ISingletonDependency
    {
        private readonly BackgroundJobChannel _channel;
        private readonly BackgroundJobsOptions _options;

        public ChannelBackgroundJobManager(BackgroundJobChannel channel, IOptions<BackgroundJobsOptions> options)
        {
            _channel = channel;
            _options = options.Value;
        }

        public async Task<bool> EnqueueAsync<TJob, TArgs>(TArgs args, CancellationToken cancellationToken = default)
            where TJob : IBackgroundJob<TArgs>
        {
            var item = new BackgroundJobItem
            {
                JobTypeName = typeof(TJob).FullName ?? typeof(TJob).Name,
                Invoke = async (sp, ct) =>
                {
                    using var scope = sp.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<TJob>();
                    await job.ExecuteAsync(args, ct).ConfigureAwait(false);
                }
            };

            if (_options.DropOldestWhenFull || _options.QueueCapacity <= 0)
            {
                await _channel.Channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return _channel.Channel.Writer.TryWrite(item)
                || await _channel.Channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)
                && _channel.Channel.Writer.TryWrite(item);
        }
    }
}
