using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Audit
{
    /// <summary>
    /// 审计日志存储抽象，业务可实现 DB / ES / 文件等持久化方案。
    /// 默认实现 <see cref="MemoryAuditLogStore"/> 仅适合开发与测试。
    /// </summary>
    public interface IAuditLogStore
    {
        /// <summary>
        /// 写入一条审计日志。
        /// </summary>
        Task SaveAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询审计日志（按时间倒序）。仅供测试/演示，生产实现按需自定义查询接口。
        /// </summary>
        Task<IReadOnlyList<AuditLogEntry>> QueryAsync(int take = 100, CancellationToken cancellationToken = default);
    }
}
