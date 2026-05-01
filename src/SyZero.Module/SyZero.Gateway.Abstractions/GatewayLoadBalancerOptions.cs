namespace SyZero.Gateway
{
    /// <summary>
    /// 网关负载均衡选项。<see cref="Type"/> 取值参考 ocelot 习惯：
    /// <see cref="RoundRobin"/> / <see cref="LeastConnection"/> / <see cref="NoLoadBalance"/>。
    /// </summary>
    public sealed class GatewayLoadBalancerOptions
    {
        public const string RoundRobin = nameof(RoundRobin);
        public const string LeastConnection = nameof(LeastConnection);
        public const string NoLoadBalance = nameof(NoLoadBalance);

        public string Type { get; set; } = RoundRobin;
    }
}
