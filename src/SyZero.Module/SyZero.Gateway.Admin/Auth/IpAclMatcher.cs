#nullable enable
using System;
using System.Collections.Generic;
using System.Net;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// IP / CIDR 列表匹配器。预解析配置项以便在请求路径上零分配快速判定。
    /// </summary>
    internal sealed class IpAclMatcher
    {
        private readonly List<IPNetwork> _networks = new();

        public bool IsEmpty => _networks.Count == 0;

        public IpAclMatcher(IEnumerable<string>? entries)
        {
            if (entries is null) return;
            foreach (var raw in entries)
            {
                if (TryParseEntry(raw, out var net))
                {
                    _networks.Add(net);
                }
            }
        }

        public bool Matches(IPAddress? ip)
        {
            if (ip is null) return false;
            // IPv4-mapped IPv6（::ffff:1.2.3.4）→ IPv4
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            for (var i = 0; i < _networks.Count; i++)
            {
                if (_networks[i].Contains(ip)) return true;
            }
            return false;
        }

        internal static bool TryParseEntry(string? raw, out IPNetwork network)
        {
            network = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim();
            try
            {
                if (s.Contains('/'))
                {
                    network = IPNetwork.Parse(s);
                    return true;
                }
                if (IPAddress.TryParse(s, out var ip))
                {
                    var prefix = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32;
                    network = new IPNetwork(ip, prefix);
                    return true;
                }
            }
            catch (FormatException) { /* 忽略坏条目 */ }
            catch (ArgumentException) { /* 忽略坏条目 */ }
            return false;
        }
    }
}
