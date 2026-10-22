using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.EventBus.Kafka;

/// <summary>
/// Kafka 连接配置（最小集合）。
/// </summary>
public sealed class SyKafkaOptions
{
    /// <summary>例如 "localhost:9092,broker2:9092"。</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string ClientId { get; set; } = "syzero";

    /// <summary>消费者组 id（消费端使用）。</summary>
    public string GroupId { get; set; } = "syzero-default";

    /// <summary>主题后缀约定：业务主题 + 重试主题 + 死信主题。</summary>
    public string RetryTopicSuffix { get; set; } = ".retry";
    public string DeadLetterTopicSuffix { get; set; } = ".dlq";
}

/// <summary>
/// 简单的 Kafka 生产抽象，业务调用 PublishAsync 即可发送字符串 payload；
/// 失败重试 / DLQ 由消费侧 <c>SyKafkaConsumerBase</c> 触发。
/// </summary>
public interface ISyKafkaProducer : IAsyncDisposable
{
    Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 带 Kafka headers 的发布。配合 <c>SyKafkaConsumerBase</c> 的「scheduled retry」机制使用：
    /// 重试消息在 header <c>sy-not-before</c> 中携带最早可处理时间（Unix ms），消费者读到后会等待至该时间再 Handle。
    /// </summary>
    Task PublishAsync(string topic, string key, string payload, System.Collections.Generic.IDictionary<string, byte[]> headers, CancellationToken cancellationToken = default);
}

internal sealed class ConfluentKafkaProducer : ISyKafkaProducer
{
    private readonly IProducer<string, string> _inner;

    public ConfluentKafkaProducer(SyKafkaOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            EnableIdempotence = true,
            Acks = Acks.All
        };
        _inner = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default)
    {
        await _inner.ProduceAsync(topic, new Message<string, string> { Key = key, Value = payload }, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAsync(string topic, string key, string payload, System.Collections.Generic.IDictionary<string, byte[]> headers, CancellationToken cancellationToken = default)
    {
        var msg = new Message<string, string> { Key = key, Value = payload };
        if (headers is { Count: > 0 })
        {
            msg.Headers = new Headers();
            foreach (var kv in headers)
            {
                msg.Headers.Add(kv.Key, kv.Value);
            }
        }
        await _inner.ProduceAsync(topic, msg, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _inner.Flush(TimeSpan.FromSeconds(5));
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
