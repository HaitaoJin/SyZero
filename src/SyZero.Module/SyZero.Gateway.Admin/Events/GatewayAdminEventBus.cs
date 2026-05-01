#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SyZero.Gateway.Admin.Events
{
    /// <summary>
    /// 进程内的网关管理事件总线：写操作触发后向所有 SSE 订阅者广播。
    /// </summary>
    public interface IGatewayAdminEventBus
    {
        /// <summary>向所有当前订阅者广播事件（不阻塞，背压时丢弃最早项）。</summary>
        void Publish(GatewayAdminEvent evt);

        /// <summary>订阅事件流。返回 (订阅 Id, ChannelReader)；调用方使用完毕必须调用 <see cref="Unsubscribe"/>。</summary>
        (Guid SubscriptionId, ChannelReader<GatewayAdminEvent> Reader) Subscribe();

        /// <summary>取消订阅并释放底层 Channel。</summary>
        void Unsubscribe(Guid subscriptionId);

        /// <summary>当前订阅者数量（监控用途）。</summary>
        int SubscriberCount { get; }
    }

    /// <summary>
    /// 默认实现：基于 <see cref="Channel{T}"/>（每订阅者一个有界 Channel，<c>DropOldest</c> 防止慢客户端积压）。
    /// </summary>
    public sealed class GatewayAdminEventBus : IGatewayAdminEventBus
    {
        private const int Capacity = 64;
        private readonly ConcurrentDictionary<Guid, Channel<GatewayAdminEvent>> _subs = new();

        public int SubscriberCount => _subs.Count;

        public void Publish(GatewayAdminEvent evt)
        {
            if (evt is null) return;
            foreach (var ch in _subs.Values)
            {
                // BoundedChannelFullMode.DropOldest 已在创建时配置 → TryWrite 始终成功
                ch.Writer.TryWrite(evt);
            }
        }

        public (Guid SubscriptionId, ChannelReader<GatewayAdminEvent> Reader) Subscribe()
        {
            var ch = Channel.CreateBounded<GatewayAdminEvent>(new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });
            var id = Guid.NewGuid();
            _subs[id] = ch;
            return (id, ch.Reader);
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            if (_subs.TryRemove(subscriptionId, out var ch))
            {
                ch.Writer.TryComplete();
            }
        }
    }
}
