#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Gateway.Admin.Auditing
{
    /// <summary>
    /// 网关管理面板的审计日志抽象。每次写入类操作（路由/Swagger CRUD、配置全量替换、备份还原等）应记录一条。
    /// </summary>
    public interface IGatewayAdminAuditLog
    {
        /// <summary>
        /// 当前实现是否启用并可写入。前端可据此显隐入口。
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// 追加一条审计记录。
        /// </summary>
        Task AppendAsync(GatewayAdminAuditEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// 倒序读取最近若干条记录。
        /// </summary>
        Task<IReadOnlyList<GatewayAdminAuditEntry>> ReadRecentAsync(int take, CancellationToken cancellationToken = default);
    }
}
