using System;

namespace SyZero.Configurations
{
    /// <summary>
    /// 配置变更事件参数。
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 触发变更通知的 UTC 时间。
        /// </summary>
        public DateTime UtcAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 触发变更的配置节（可选，由 publisher 传入）。
        /// </summary>
        public string? Section { get; set; }
    }

    /// <summary>
    /// 配置热更新通知抽象。
    /// 默认实现监听 <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> 的 ChangeToken，
    /// 与 Nacos / Consul / appsettings.json 等支持 reload 的 provider 自动联动。
    /// </summary>
    public interface IConfigurationChangedNotifier
    {
        /// <summary>
        /// 当任意配置发生变更时触发。
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> Changed;

        /// <summary>
        /// 由 provider 主动触发一次变更（用于不走 IConfiguration 的自定义来源）。
        /// </summary>
        void Notify(string? section = null);
    }
}
