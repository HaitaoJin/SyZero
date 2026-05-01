#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关配置持久化抽象。供管理面板（SyZero.Gateway.Admin）读写网关配置使用。
    /// 具体实现决定存储介质（文件、DB、Consul 等）。
    /// </summary>
    public interface IGatewayConfigurationStore
    {
        /// <summary>
        /// 当前配置存储位置的可读描述（文件路径、DB 连接、Consul Key 等），用于诊断展示。
        /// </summary>
        string ConfigPath { get; }

        /// <summary>
        /// 最近一次备份时间。未实现备份的存储可返回 <c>null</c>。
        /// </summary>
        DateTime? LastBackupTime { get; }

        /// <summary>
        /// 读取当前配置。
        /// </summary>
        Task<GatewayConfiguration> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 持久化配置；实现内部应负责备份、原子写入并触发 <see cref="IGatewayConfigurationApplier"/> 完成热下发。
        /// </summary>
        Task WriteAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 强制重新加载配置并下发到运行中的网关。
        /// </summary>
        Task ReloadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 列出所有可用备份（按创建时间倒序）。默认返回空列表，
        /// 表示当前实现不支持备份枚举（如 DB / Consul 等无文件备份语义的实现）。
        /// </summary>
        Task<IReadOnlyList<GatewayConfigurationBackup>> ListBackupsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GatewayConfigurationBackup>>(Array.Empty<GatewayConfigurationBackup>());

        /// <summary>
        /// 读取指定备份的配置内容。默认返回 <c>null</c>，表示不存在或当前实现不支持。
        /// </summary>
        Task<GatewayConfiguration?> ReadBackupAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<GatewayConfiguration?>(null);

        /// <summary>
        /// 将指定备份还原为当前配置。还原会先备份当前配置再写入，并触发热下发。
        /// 默认抛出 <see cref="NotSupportedException"/>。
        /// </summary>
        Task RestoreBackupAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("当前 IGatewayConfigurationStore 实现不支持备份还原。");

        /// <summary>
        /// 删除指定备份。默认抛出 <see cref="NotSupportedException"/>。
        /// </summary>
        Task DeleteBackupAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("当前 IGatewayConfigurationStore 实现不支持备份删除。");
    }
}
