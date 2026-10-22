using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SyZero.Runtime.Security;
using SyZero.Service;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace SyZero.Gateway.YARP
{
    /// <summary>
    /// 基于 SyZero <see cref="IServiceManagement"/> 抽象的 YARP 目标解析器。
    /// 把形如 <c>discovery://{serviceName}</c> 的占位 destination 展开为
    /// 真实的 <c>{scheme}://{address}:{port}</c> 列表，从而打通 Consul / Nacos / Redis / Local /
    /// DB 等 SyZero 服务发现后端 与 YARP 的桥梁。
    /// </summary>
    /// <remarks>
    /// <para>使用约定：</para>
    /// <list type="bullet">
    ///   <item>当 <see cref="Gateway.GatewayRoute.ServiceName"/> 不为空时，<see cref="YarpConfigMapper"/>
    ///   会为对应集群生成一个 <c>discovery://{ServiceName}</c> 占位 destination。</item>
    ///   <item>本解析器在 YARP 加载/刷新集群时被调用，把占位地址展开为
    ///   <see cref="IServiceManagement.GetHealthyServices(string)"/> 返回的健康实例列表。</item>
    ///   <item>解析时会订阅 <see cref="IServiceManagement.Subscribe"/>；当服务实例上下线时
    ///   通过 <see cref="IChangeToken"/> 通知 YARP 仅刷新对应 cluster，无需重新加载整份网关配置。</item>
    ///   <item>非 <c>discovery</c> scheme 的 destination 原样透传，便于与静态 cluster 共存。</item>
    /// </list>
    /// </remarks>
    public sealed class SyZeroYarpServiceDiscoveryDestinationResolver : IDestinationResolver, IDisposable
    {
        private const string DiscoveryScheme = "discovery";

        private readonly IServiceManagement _serviceManagement;
        private readonly ILogger<SyZeroYarpServiceDiscoveryDestinationResolver> _logger;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _changeTokens = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _subscribed = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public SyZeroYarpServiceDiscoveryDestinationResolver(
            IServiceManagement serviceManagement,
            ILogger<SyZeroYarpServiceDiscoveryDestinationResolver> logger)
        {
            _serviceManagement = serviceManagement ?? throw new ArgumentNullException(nameof(serviceManagement));
            _logger = logger;
        }

        public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(
            IReadOnlyDictionary<string, DestinationConfig> destinations,
            CancellationToken cancellationToken)
        {
            if (destinations is null || destinations.Count == 0)
            {
                return new ResolvedDestinationCollection(destinations ?? new Dictionary<string, DestinationConfig>(), changeToken: null);
            }

            var resolved = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);
            var discoveredServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in destinations)
            {
                if (!TryParseDiscoveryAddress(kv.Value?.Address, out var serviceName))
                {
                    resolved[kv.Key] = kv.Value!;
                    continue;
                }

                discoveredServices.Add(serviceName!);

                List<ServiceInfo>? instances = null;
                try
                {
                    instances = await _serviceManagement.GetHealthyServices(serviceName!).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析服务实例失败：{ServiceName}", serviceName);
                }

                if (instances is null || instances.Count == 0)
                {
                    _logger.LogDebug("服务 {ServiceName} 暂无健康实例，YARP 将向该集群返回 503。", serviceName);
                    continue;
                }

                var idx = 0;
                foreach (var instance in instances)
                {
                    var address = BuildAddress(instance);
                    if (string.IsNullOrEmpty(address))
                    {
                        continue;
                    }

                    var name = $"{kv.Key}_{idx++}";
                    resolved[name] = new DestinationConfig
                    {
                        Address = address,
                        Metadata = kv.Value!.Metadata,
                        Health = kv.Value.Health,
                        Host = kv.Value.Host
                    };
                }
            }

            // 为本次涉及到的服务名建立"变更通知"通道
            IChangeToken? changeToken = null;
            if (discoveredServices.Count > 0)
            {
                var tokens = new List<IChangeToken>(discoveredServices.Count);
                foreach (var sn in discoveredServices)
                {
                    EnsureSubscribed(sn);
                    var cts = _changeTokens.GetOrAdd(sn, _ => new CancellationTokenSource());
                    tokens.Add(new CancellationChangeToken(cts.Token));
                }

                changeToken = tokens.Count == 1 ? tokens[0] : new CompositeChangeToken(tokens);
            }

            return new ResolvedDestinationCollection(resolved, changeToken);
        }

        private void EnsureSubscribed(string serviceName)
        {
            if (!_subscribed.TryAdd(serviceName, 0))
            {
                return;
            }

            try
            {
                // fire-and-forget：订阅本身一般是同步注册回调
                _ = _serviceManagement.Subscribe(serviceName, _ => SignalChange(serviceName));
            }
            catch (Exception ex)
            {
                _subscribed.TryRemove(serviceName, out _);
                _logger.LogWarning(ex, "订阅服务变更失败：{ServiceName}", serviceName);
            }
        }

        private void SignalChange(string serviceName)
        {
            if (_changeTokens.TryRemove(serviceName, out var oldCts))
            {
                try { oldCts.Cancel(); }
                catch (ObjectDisposedException) { }
                oldCts.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sn in _subscribed.Keys.ToList())
            {
                try { _ = _serviceManagement.Unsubscribe(sn); } catch { /* best-effort */ }
            }
            _subscribed.Clear();

            foreach (var cts in _changeTokens.Values)
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
                cts.Dispose();
            }
            _changeTokens.Clear();
        }

        private static bool TryParseDiscoveryAddress(string? address, out string? serviceName)
        {
            serviceName = null;
            if (string.IsNullOrWhiteSpace(address)) return false;
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)) return false;
            if (!string.Equals(uri.Scheme, DiscoveryScheme, StringComparison.OrdinalIgnoreCase)) return false;

            serviceName = string.IsNullOrEmpty(uri.Host) ? null : uri.Host;
            return !string.IsNullOrEmpty(serviceName);
        }

        private static string BuildAddress(ServiceInfo instance)
        {
            if (instance is null || string.IsNullOrWhiteSpace(instance.ServiceAddress) || instance.ServicePort <= 0)
            {
                return string.Empty;
            }

            var scheme = MapScheme(instance.ServiceProtocol);
            return $"{scheme}://{instance.ServiceAddress}:{instance.ServicePort}/";
        }

        private static string MapScheme(ProtocolType protocol)
        {
            return protocol switch
            {
                ProtocolType.HTTPS => "https",
                ProtocolType.GRPC => "http", // YARP 通过 H2 over HTTP 透传 gRPC
                _ => "http"
            };
        }
    }
}

