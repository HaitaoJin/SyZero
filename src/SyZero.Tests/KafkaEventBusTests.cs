using Microsoft.Extensions.DependencyInjection;
using SyZero.EventBus.Kafka;
using Xunit;

namespace SyZero.Tests;

public class KafkaEventBusTests
{
    [Fact]
    public void AddSyZeroKafka_RegistersOptionsAndProducer()
    {
        var services = new ServiceCollection();
        services.AddSyZeroKafka(o =>
        {
            o.BootstrapServers = "localhost:9092";
            o.ClientId = "test";
            o.GroupId = "test-grp";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<SyKafkaOptions>();
        Assert.Equal("localhost:9092", options.BootstrapServers);
        Assert.Equal(".retry", options.RetryTopicSuffix);
        Assert.Equal(".dlq", options.DeadLetterTopicSuffix);

        // 不实际连接 broker，只验证类型解析（生产者会延迟初始化连接）。
        var producerType = sp.GetService<ISyKafkaProducer>();
        Assert.NotNull(producerType);
    }

    [Fact]
    public void Backoff_ExponentialWithJitter_AndCapped()
    {
        var baseDelay = System.TimeSpan.FromSeconds(1);
        var max = System.TimeSpan.FromSeconds(30);

        // n=1：raw=1s，抖动 [0.8, 1.2)
        var d1 = SyKafkaBackoff.Compute(1, baseDelay, max);
        Assert.InRange(d1.TotalMilliseconds, 800, 1200);

        // n=4：raw=8s，抖动 [6.4, 9.6)s
        var d4 = SyKafkaBackoff.Compute(4, baseDelay, max);
        Assert.InRange(d4.TotalMilliseconds, 6400, 9600);

        // n=10：raw=512s，被 max=30s 封顶，抖动 [24, 36)s
        var d10 = SyKafkaBackoff.Compute(10, baseDelay, max);
        Assert.InRange(d10.TotalMilliseconds, 24_000, 36_000);
    }
}
