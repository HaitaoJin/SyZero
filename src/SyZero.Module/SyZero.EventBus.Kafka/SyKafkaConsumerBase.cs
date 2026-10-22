using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.EventBus.Kafka;

/// <summary>
/// Kafka 消费者基类：处理失败后按 <c>RetryTopic</c> + <c>DeadLetterTopic</c> 约定自动转发。
/// 派生类只需实现 <see cref="HandleAsync"/> 并通过 <c>AddHostedService</c> 注册。
/// 重试支持「scheduled retry」：失败时按 <see cref="RetryBaseDelay"/> 指数退避（带 ±20% 抖动），
/// 上限为 <see cref="RetryMaxDelay"/>，把 <c>not-before</c> 时间戳写入 header；
/// 消费 retry topic 时若未到时间会 <c>Task.Delay</c> 至时再调用 <see cref="HandleAsync"/>。
/// </summary>
public abstract class SyKafkaConsumerBase : BackgroundService
{
    private const string RetryHeader = "sy-retry";
    private const string NotBeforeHeader = "sy-not-before";

    protected SyKafkaOptions Options { get; }
    protected ISyKafkaProducer Producer { get; }
    protected ILogger Logger { get; }
    protected abstract string Topic { get; }
    protected virtual int MaxRetry => 3;
    /// <summary>第一次重试的基础延迟。</summary>
    protected virtual TimeSpan RetryBaseDelay => TimeSpan.FromSeconds(2);
    /// <summary>退避时间上限。</summary>
    protected virtual TimeSpan RetryMaxDelay => TimeSpan.FromMinutes(5);

    protected SyKafkaConsumerBase(SyKafkaOptions options, ISyKafkaProducer producer, ILogger logger)
    {
        Options = options;
        Producer = producer;
        Logger = logger;
    }

    /// <summary>处理一条消息；抛出异常将触发重试 → 死信流程。</summary>
    protected abstract Task HandleAsync(string key, string payload, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = Options.BootstrapServers,
            GroupId = Options.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(new[] { Topic, Topic + Options.RetryTopicSuffix });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    if (result?.Message == null) continue;

                    // scheduled retry：若 retry topic 上的消息尚未到「not-before」时间，则 sleep 至时再处理
                    var notBefore = ParseNotBefore(result.Message);
                    if (notBefore.HasValue)
                    {
                        var wait = notBefore.Value - DateTimeOffset.UtcNow;
                        if (wait > TimeSpan.Zero)
                        {
                            try
                            {
                                await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { break; }
                        }
                    }

                    await HandleAsync(result.Message.Key, result.Message.Value, stoppingToken).ConfigureAwait(false);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) when (result != null)
                {
                    Logger.LogWarning(ex, "Kafka 消费失败 topic={Topic} key={Key}", result.Topic, result.Message.Key);
                    var retryCount = ParseRetryCount(result.Message);
                    var nextRetry = retryCount + 1;
                    if (nextRetry >= MaxRetry)
                    {
                        var dlqHeaders = new Dictionary<string, byte[]>
                        {
                            [RetryHeader] = System.Text.Encoding.UTF8.GetBytes(nextRetry.ToString()),
                            ["sy-last-error"] = System.Text.Encoding.UTF8.GetBytes(ex.Message ?? string.Empty)
                        };
                        await Producer.PublishAsync(Topic + Options.DeadLetterTopicSuffix, result.Message.Key, result.Message.Value, dlqHeaders, stoppingToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var delay = ComputeBackoff(nextRetry);
                        var notBeforeMs = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeMilliseconds();
                        var headers = new Dictionary<string, byte[]>
                        {
                            [RetryHeader] = System.Text.Encoding.UTF8.GetBytes(nextRetry.ToString()),
                            [NotBeforeHeader] = System.Text.Encoding.UTF8.GetBytes(notBeforeMs.ToString())
                        };
                        await Producer.PublishAsync(Topic + Options.RetryTopicSuffix, result.Message.Key, result.Message.Value, headers, stoppingToken).ConfigureAwait(false);
                    }
                    consumer.Commit(result);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private TimeSpan ComputeBackoff(int retryCount) => SyKafkaBackoff.Compute(retryCount, RetryBaseDelay, RetryMaxDelay);

    private static int ParseRetryCount(Message<string, string> message)
    {
        if (message.Headers == null) return 0;
        if (message.Headers.TryGetLastBytes(RetryHeader, out var bytes) && int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var n))
        {
            return n;
        }
        return 0;
    }

    private static DateTimeOffset? ParseNotBefore(Message<string, string> message)
    {
        if (message.Headers == null) return null;
        if (message.Headers.TryGetLastBytes(NotBeforeHeader, out var bytes)
            && long.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }
        return null;
    }
}
