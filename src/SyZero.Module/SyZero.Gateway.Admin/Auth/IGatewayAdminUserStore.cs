#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 管理台「文件型」账号存储（运行时 CRUD 的目标，对应 users.json）。
    /// appsettings 中的 <c>Gateway:Admin:Users[]</c> 不在此接口范围内（只读，由 <see cref="IUserResolver"/> 合并）。
    /// </summary>
    public interface IGatewayAdminUserStore
    {
        /// <summary>读取全部文件账号（不含密码字段时由实现自行决定；本接口返回完整数据）。</summary>
        Task<IReadOnlyList<GatewayAdminUser>> ListAsync(CancellationToken ct);

        /// <summary>新增账号。Name 冲突时返回 false。</summary>
        Task<bool> AddAsync(GatewayAdminUser user, CancellationToken ct);

        /// <summary>更新账号（按 Name 定位）。可选改密码 / 角色，传 null 则不改。返回 false 表示未找到。</summary>
        Task<bool> UpdateAsync(string name, string? newPassword, string? newRole, CancellationToken ct);

        /// <summary>按 Name 删除。返回 false 表示未找到。</summary>
        Task<bool> RemoveAsync(string name, CancellationToken ct);

        /// <summary>按 Name 查找单条。</summary>
        Task<GatewayAdminUser?> FindAsync(string name, CancellationToken ct);
    }

    /// <summary>
    /// 合并 appsettings.Users[] 与 <see cref="IGatewayAdminUserStore"/> 的视图。
    /// 当存在重名时 appsettings 优先（同名 file 项被忽略）。
    /// </summary>
    public interface IUserResolver
    {
        /// <summary>列出全部账号（带 IsBuiltIn 标记）。</summary>
        Task<IReadOnlyList<GatewayAdminUser>> ListAsync(CancellationToken ct);

        /// <summary>按 Name 查找；未找到返回 null。</summary>
        Task<GatewayAdminUser?> FindAsync(string name, CancellationToken ct);

        /// <summary>校验用户名 + 密码（明文比较，固定时间）。返回匹配到的用户（带 IsBuiltIn / Role），否则 null。</summary>
        Task<GatewayAdminUser?> ValidateAsync(string name, string password, CancellationToken ct);

        /// <summary>统计 Admin 角色账号数（用于 UI / 业务保护）。</summary>
        Task<int> CountAdminsAsync(CancellationToken ct);
    }
}
