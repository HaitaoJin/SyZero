using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyZero.BackgroundJobs;
using Xunit;

namespace SyZero.Tests;

public class BackgroundJobsTests
{
    public class PingArgs
    {
        public TaskCompletionSource<int> Tcs { get; set; } = default!;
        public int Value { get; set; }
    }

    public class PingJob : IBackgroundJob<PingArgs>
    {
        public Task ExecuteAsync(PingArgs args, CancellationToken ct = default)
        {
            args.Tcs.TrySetResult(args.Value);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task EnqueueAsync_RunsJob_ViaWorker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSyZeroBackgroundJobs(configuration: null, configure: opt =>
        {
            opt.QueueCapacity = 16;
            opt.WorkerCount = 1;
        });
        services.AddBackgroundJob<PingJob>();
        await using var sp = services.BuildServiceProvider();

        // 启动 hosted service
        var hosted = sp.GetServices<IHostedService>();
        foreach (var h in hosted) await h.StartAsync(CancellationToken.None);

        try
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var manager = sp.GetRequiredService<IBackgroundJobManager>();
            var ok = await manager.EnqueueAsync<PingJob, PingArgs>(new PingArgs { Tcs = tcs, Value = 42 });
            Assert.True(ok);

            var done = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(tcs.Task, done);
            Assert.Equal(42, await tcs.Task);
        }
        finally
        {
            foreach (var h in hosted) await h.StopAsync(CancellationToken.None);
        }
    }
}
