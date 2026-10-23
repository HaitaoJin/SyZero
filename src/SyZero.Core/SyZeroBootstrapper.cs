using System;
using System.Text.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SyZero.Service;
using Microsoft.Extensions.Options;
using SyZero.Configurations;

namespace SyZero
{
    /// <summary>
    /// SyZero 框架启动 / 关闭钩子，
    /// 通过 <see cref="IHostedService"/> 在宿主启动 / 停止阶段以异步方式
    /// 完成服务注册中心的注册与注销，避免在 <c>UseSyZero</c> 中同步阻塞调用异步方法。
    /// </summary>
    internal sealed class SyZeroBootstrapper : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyZeroBootstrapper> _logger;
        private readonly SyZeroServerOptions _serverOptions;
        private string? _registeredServiceId;

        public SyZeroBootstrapper(IServiceProvider serviceProvider, IOptions<SyZeroServerOptions> serverOptions, ILogger<SyZeroBootstrapper>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _serverOptions = serverOptions.Value;
            _logger = logger ?? NullLogger<SyZeroBootstrapper>.Instance;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var serverOptions = _serverOptions;
                if (serverOptions == null || string.IsNullOrEmpty(serverOptions.Name))
                {
                    _logger.LogInformation("SyZero: 未配置服务信息，跳过服务注册");
                    return;
                }

                var serviceManagement = _serviceProvider.GetService<IServiceManagement>();
                if (serviceManagement == null)
                {
                    _logger.LogInformation("SyZero: 未找到服务管理实现，跳过服务注册");
                    return;
                }

                var serviceId = $"{serverOptions.Name}-{serverOptions.Ip}-{serverOptions.Port}";
                var serviceInfo = new ServiceInfo
                {
                    ServiceID = serviceId,
                    ServiceName = serverOptions.Name,
                    ServiceAddress = serverOptions.Ip ?? string.Empty,
                    ServicePort = serverOptions.Port,
                    ServiceProtocol = serverOptions.Protocol,
                    Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
                    IsHealthy = true,
                    Enabled = true,
                    Weight = 1.0,
                    RegisterTime = DateTime.Now,
                    LastHeartbeat = DateTime.Now,
                    HealthCheckUrl = $"{serverOptions.Protocol.ToString().ToLower()}://{serverOptions.Ip}:{serverOptions.Port}/health",
                    Tags = new System.Collections.Generic.List<string>(),
                    Metadata = new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["startTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                    }
                };
                AppendApiEndpointMetadata(serviceInfo);

                await serviceManagement.RegisterService(serviceInfo).ConfigureAwait(false);
                _registeredServiceId = serviceId;
                _logger.LogInformation("SyZero: 服务 [{ServiceName}] 注册成功 ({Ip}:{Port})", serverOptions.Name, serverOptions.Ip, serverOptions.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyZero: 服务注册失败");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_registeredServiceId))
                {
                    return;
                }

                var serviceManagement = _serviceProvider.GetService<IServiceManagement>();
                if (serviceManagement == null)
                {
                    return;
                }

                await serviceManagement.DeregisterService(_registeredServiceId).ConfigureAwait(false);
                _logger.LogInformation("SyZero: 服务 [{ServiceId}] 已注销", _registeredServiceId);
                _registeredServiceId = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyZero: 服务注销失败");
            }
        }

        private void AppendApiEndpointMetadata(ServiceInfo serviceInfo)
        {
            var endpoints = ServiceEndpointMetadataCollector.Collect(_serviceProvider);
            if (endpoints.Count == 0)
            {
                return;
            }

            serviceInfo.Metadata[ServiceMetadataKeys.ApiEndpoints] = JsonSerializer.Serialize(endpoints);
            serviceInfo.Metadata[ServiceMetadataKeys.ApiEndpointCount] = endpoints.Count.ToString();
        }
    }
}
