using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SyZero.Gateway;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;

namespace SyZero.Gateway.YARP
{
    /// <summary>
    /// <see cref="GatewayConfiguration"/>（Ocelot 风格 schema）与 YARP 原生 <see cref="RouteConfig"/> /
    /// <see cref="ClusterConfig"/> 之间的双向映射。
    /// </summary>
    /// <remarks>
    /// 为保证多后端可互换，SyZero 中性 schema 沿用 Ocelot 命名（UpstreamPathTemplate / DownstreamPathTemplate / ServiceName 等）。
    /// 本类负责把 Ocelot 的 catch-all 模板（<c>{everything}</c>）转写成 YARP 风格（<c>{**catchAll}</c>），
    /// 并把按 ServiceName 分组的路由聚合成 YARP 集群（每个 ServiceName 一个 cluster）。
    /// 若路由未指定 ServiceName，则按 UpstreamPathTemplate 派生唯一 cluster id。
    /// </remarks>
    internal static class YarpConfigMapper
    {
        private static readonly Regex CatchAllSegment = new Regex(@"\{(?<name>[^/{}]+)\}", RegexOptions.Compiled);

        public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Map(GatewayConfiguration configuration)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            var routes = new List<RouteConfig>();
            var clusters = new Dictionary<string, ClusterConfig>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < configuration.Routes.Count; i++)
            {
                var src = configuration.Routes[i];
                if (src.Disabled)
                {
                    // 禁用路由不参与 YARP 下发，仅保留在 configuration.json 中。
                    continue;
                }
                var clusterId = ResolveClusterId(src, i);
                if (!clusters.ContainsKey(clusterId))
                {
                    clusters[clusterId] = BuildCluster(clusterId, src);
                }

                routes.Add(BuildRoute(routeIndex: i, source: src, clusterId: clusterId));
            }

            return (routes, clusters.Values.ToList());
        }

        private static string ResolveClusterId(GatewayRoute route, int index)
        {
            if (!string.IsNullOrWhiteSpace(route.ServiceName))
            {
                return route.ServiceName!;
            }

            // 没有 ServiceName 时回落到匿名 cluster；保证唯一即可。
            return "cluster_" + index.ToString();
        }

        private static RouteConfig BuildRoute(int routeIndex, GatewayRoute source, string clusterId)
        {
            var match = new RouteMatch
            {
                Path = ConvertCatchAll(source.UpstreamPathTemplate),
                Methods = source.UpstreamHttpMethod is { Count: > 0 } methods
                    ? methods.Select(m => m.ToUpperInvariant()).Distinct().ToArray()
                    : null
            };

            var transforms = new List<IReadOnlyDictionary<string, string>>();
            if (!string.IsNullOrWhiteSpace(source.DownstreamPathTemplate))
            {
                transforms.Add(new Dictionary<string, string>
                {
                    ["PathPattern"] = ConvertCatchAll(source.DownstreamPathTemplate)
                });
            }

            return new RouteConfig
            {
                RouteId = $"route_{routeIndex}",
                ClusterId = clusterId,
                Match = match,
                Transforms = transforms.Count == 0 ? null : transforms
            };
        }

        private static ClusterConfig BuildCluster(string clusterId, GatewayRoute source)
        {
            // 中性 schema 不直接携带 host:port 列表（Ocelot 是从 ServiceDiscovery 解析）。
            // 约定：当存在 ServiceName 时，emit 一个 discovery:// 占位 destination，
            // 由 SyZeroYarpServiceDiscoveryDestinationResolver 在运行时展开为真实实例；
            // 若 ServiceName 为空，则退化为 localhost 直连占位符（开发用）。
            var destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(source.ServiceName))
            {
                destinations["d_discovery"] = new DestinationConfig
                {
                    Address = $"discovery://{source.ServiceName}/"
                };
            }
            else
            {
                destinations["d_default"] = new DestinationConfig
                {
                    Address = $"{source.DownstreamScheme ?? "http"}://localhost/"
                };
            }

            return new ClusterConfig
            {
                ClusterId = clusterId,
                LoadBalancingPolicy = MapLoadBalancing(source.LoadBalancerOptions?.Type),
                Destinations = destinations
            };
        }

        private static string? MapLoadBalancing(string? type)
        {
            return type switch
            {
                GatewayLoadBalancerOptions.RoundRobin => LoadBalancingPolicies.RoundRobin,
                GatewayLoadBalancerOptions.LeastConnection => LoadBalancingPolicies.LeastRequests,
                GatewayLoadBalancerOptions.NoLoadBalance => LoadBalancingPolicies.FirstAlphabetical,
                _ => null
            };
        }

        private static string ConvertCatchAll(string template)
        {
            // Ocelot：/user-service/{everything}  ->  YARP：/user-service/{**catchAll}
            // 单段占位符直接保留（{id} 在 YARP 里同样合法）。
            if (string.IsNullOrEmpty(template)) return string.Empty;
            return CatchAllSegment.Replace(template, m =>
            {
                var name = m.Groups["name"].Value;
                if (name.Equals("everything", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("catchAll", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("**catchAll", StringComparison.OrdinalIgnoreCase))
                {
                    return "{**catchAll}";
                }
                return m.Value;
            });
        }
    }
}
