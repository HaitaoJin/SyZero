using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using SyZero.Gateway;

namespace SyZero.Gateway.Ocelot
{
    /// <summary>
    /// 基于 Ocelot 的 <see cref="IGateway"/> 实现：从 Ocelot 内部 <see cref="IFileConfigurationRepository"/>
    /// 读取当前生效的 <see cref="FileConfiguration"/> 并映射为 SyZero 的中性 <see cref="GatewayConfiguration"/>。
    /// </summary>
    public sealed class OcelotGateway : IGateway
    {
        private readonly IFileConfigurationRepository _repository;

        public OcelotGateway(IFileConfigurationRepository repository)
        {
            _repository = repository;
        }

        public string ProviderName => "Ocelot";

        public async Task<GatewayConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var response = await _repository.Get().ConfigureAwait(false);
            if (response is null || response.IsError || response.Data is null)
            {
                return new GatewayConfiguration();
            }
            return MapToGatewayConfiguration(response.Data);
        }

        internal static GatewayConfiguration MapToGatewayConfiguration(FileConfiguration file)
        {
            var dto = new GatewayConfiguration();
            foreach (var r in file.Routes)
            {
                dto.Routes.Add(new GatewayRoute
                {
                    UpstreamPathTemplate = r.UpstreamPathTemplate,
                    UpstreamHttpMethod = r.UpstreamHttpMethod?.ToList() ?? new List<string>(),
                    DownstreamPathTemplate = r.DownstreamPathTemplate,
                    DownstreamScheme = r.DownstreamScheme,
                    ServiceName = string.IsNullOrEmpty(r.ServiceName) ? null : r.ServiceName,
                    LoadBalancerOptions = new GatewayLoadBalancerOptions
                    {
                        Type = string.IsNullOrEmpty(r.LoadBalancerOptions?.Type)
                            ? GatewayLoadBalancerOptions.RoundRobin
                            : r.LoadBalancerOptions!.Type
                    }
                });
            }
            return dto;
        }
    }
}
