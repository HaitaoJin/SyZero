using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SyZero.BackgroundJobs
{
    /// <summary>从 <see cref="BackgroundJobChannel"/> 取任务并执行的 Worker。可启动多个并发实例。</summary>
    public class BackgroundJobWorker : BackgroundService
    {
        private readonly BackgroundJobChannel _channel;
        private readonly IServiceProvider _services;
        private readonly ILogger<BackgroundJobWorker> _logger;
        private readonly BackgroundJobsOptions _options;

        public BackgroundJobWorker(
            BackgroundJobChannel channel,
            IServiceProvider services,
            ILogger<BackgroundJobWorker> logger,
            IOptions<BackgroundJobsOptions> options)
        {
            _channel = channel;
            _services = services;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workers = Math.Max(1, _options.WorkerCount);
            var tasks = new Task[workers];
            for (var i = 0; i < workers; i++)
            {
                var workerIndex = i;
                tasks[i] = Task.Run(() => RunLoopAsync(workerIndex, stoppingToken), stoppingToken);
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task RunLoopAsync(int index, CancellationToken ct)
        {
            await foreach (var item in _channel.Channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await item.Invoke(_services, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BgJobs#{Worker}] 任务 {Job} 执行失败", index, item.JobTypeName);
                }
            }
        }
    }
}
