using Consul;
using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;

namespace SyZero.Gateway.Ocelot
{
    /// <summary>
    /// 自定义 Ocelot Consul 服务构建器，使用 <c>Service.Address</c> 作为下游主机，
    /// 而非 Consul Node 的 Address，匹配 SyZero 服务注册习惯。
    /// </summary>
    public class SyZeroConsulServiceBuilder : DefaultConsulServiceBuilder
    {
        public SyZeroConsulServiceBuilder(
            IHttpContextAccessor contextAccessor,
            IConsulClientFactory clientFactory,
            IOcelotLoggerFactory loggerFactory)
            : base(contextAccessor, clientFactory, loggerFactory)
        {
        }

        protected override string GetDownstreamHost(ServiceEntry entry, Node node)
        {
            return entry.Service.Address;
        }
    }
}
