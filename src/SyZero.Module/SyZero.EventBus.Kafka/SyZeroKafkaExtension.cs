using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.EventBus.Kafka;
using System;

namespace SyZero;

/// <summary>SyZero.EventBus.Kafka 的 DI 扩展。</summary>
public static class SyZeroKafkaExtension
{
    /// <summary>
    /// 注册 Kafka 生产者与配置。消费者请继承 <see cref="SyKafkaConsumerBase"/> 并通过 <c>AddHostedService</c> 注册。
    /// </summary>
    public static IServiceCollection AddSyZeroKafka(this IServiceCollection services, Action<SyKafkaOptions> configure)
    {
        var options = new SyKafkaOptions();
        configure(options);
        services.AddSingleton(options);
        services.TryAddSingleton<ISyKafkaProducer, ConfluentKafkaProducer>();
        return services;
    }
}
