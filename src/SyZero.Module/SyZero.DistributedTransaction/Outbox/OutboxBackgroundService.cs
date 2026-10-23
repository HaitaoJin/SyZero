using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyZero.EventBus;

namespace SyZero.DistributedTransaction
{
    /// <summary>
    /// Outbox 后台投递服务：定时从 <see cref="IOutboxStore"/> 拉待发送消息，发布到 <see cref="IEventBus"/>。
    /// </summary>
    public class OutboxBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OutboxOptions _options;
        private readonly ILogger<OutboxBackgroundService> _logger;

        public OutboxBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxOptions>? options,
            ILogger<OutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _options = options?.Value ?? new OutboxOptions();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox loop iteration failed");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var bus = scope.ServiceProvider.GetService<IEventBus>();
            if (bus == null)
            {
                _logger.LogWarning("IEventBus 未注册，Outbox 跳过本轮投递");
                return;
            }

            var owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
            var pending = await store.ClaimPendingAsync(
                _options.BatchSize,
                owner,
                TimeSpan.FromSeconds(Math.Max(10, _options.LeaseSeconds)),
                ct).ConfigureAwait(false);
            foreach (var msg in pending)
            {
                ct.ThrowIfCancellationRequested();
                if (msg.RetryCount >= _options.MaxRetryCount)
                {
                    continue;
                }
                try
                {
                    bus.Publish(msg.EventName, msg.PayloadJson);
                    await store.MarkPublishedAsync(msg.Id, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox 发布失败 {EventName} {Id}", msg.EventName, msg.Id);
                    await store.MarkFailedAsync(msg.Id, ex.Message, ct).ConfigureAwait(false);
                }
            }
        }
    }
}
