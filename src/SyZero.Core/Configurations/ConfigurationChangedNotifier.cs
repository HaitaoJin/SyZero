using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SyZero.Dependency;

namespace SyZero.Configurations
{
    /// <summary>
    /// 默认实现：基于 <see cref="IConfiguration.GetReloadToken"/> + <see cref="ChangeToken.OnChange"/>。
    /// 对支持 reloadOnChange 的 provider（Nacos / Consul / appsettings.json）自动生效。
    /// </summary>
    public class ConfigurationChangedNotifier : IConfigurationChangedNotifier, ISingletonDependency, IDisposable
    {
        private readonly ILogger<ConfigurationChangedNotifier>? _logger;
        private readonly IDisposable? _registration;

        public event EventHandler<ConfigurationChangedEventArgs>? Changed;

        public ConfigurationChangedNotifier(IConfiguration configuration, ILogger<ConfigurationChangedNotifier>? logger = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            _registration = ChangeToken.OnChange(configuration.GetReloadToken, () => Notify());
        }

        public void Notify(string? section = null)
        {
            try
            {
                Changed?.Invoke(this, new ConfigurationChangedEventArgs { Section = section });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Configuration change handler threw");
            }
        }

        public void Dispose()
        {
            _registration?.Dispose();
        }
    }
}
