using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace SyZero.Gateway.YARP
{
    /// <summary>
    /// 内存型的 YARP 配置提供者，支持运行时热更新。
    /// 由 <see cref="YarpConfigurationApplier"/> 调用 <see cref="Update"/> 进行热下发。
    /// </summary>
    public sealed class SyZeroYarpProxyConfigProvider : IProxyConfigProvider
    {
        private volatile InMemoryConfig _current;

        public SyZeroYarpProxyConfigProvider()
        {
            _current = new InMemoryConfig(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());
        }

        public IProxyConfig GetConfig() => _current;

        public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            if (routes is null) throw new ArgumentNullException(nameof(routes));
            if (clusters is null) throw new ArgumentNullException(nameof(clusters));

            var old = _current;
            _current = new InMemoryConfig(routes, clusters);
            old.SignalChange();
        }

        private sealed class InMemoryConfig : IProxyConfig
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public InMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            {
                Routes = routes;
                Clusters = clusters;
                ChangeToken = new CancellationChangeToken(_cts.Token);
            }

            public IReadOnlyList<RouteConfig> Routes { get; }

            public IReadOnlyList<ClusterConfig> Clusters { get; }

            public IChangeToken ChangeToken { get; }

            public void SignalChange()
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}
