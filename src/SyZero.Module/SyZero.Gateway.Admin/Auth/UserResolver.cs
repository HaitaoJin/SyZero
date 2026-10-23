#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 默认账号解析器：合并 appsettings 中的 <see cref="GatewayAdminUsersOptions.Users"/>（IsBuiltIn=true）
    /// 与 <see cref="IGatewayAdminUserStore"/> 中的文件账号（IsBuiltIn=false）。
    /// 重名时 appsettings 优先，文件中同名项被忽略并写入告警日志。
    /// </summary>
    public sealed class UserResolver : IUserResolver
    {
        private readonly IOptionsMonitor<GatewayAdminUsersOptions> _options;
        private readonly IGatewayAdminUserStore _fileStore;
        private readonly ILogger<UserResolver>? _logger;

        public UserResolver(
            IOptionsMonitor<GatewayAdminUsersOptions> options,
            IGatewayAdminUserStore fileStore,
            ILogger<UserResolver>? logger = null)
        {
            _options = options;
            _fileStore = fileStore;
            _logger = logger;
        }

        public async Task<IReadOnlyList<GatewayAdminUser>> ListAsync(CancellationToken ct)
        {
            var list = new List<GatewayAdminUser>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // appsettings 优先
            foreach (var u in _options.CurrentValue.Users ?? new List<GatewayAdminUser>())
            {
                if (string.IsNullOrWhiteSpace(u.Name)) continue;
                if (!seen.Add(u.Name)) continue;
                list.Add(new GatewayAdminUser
                {
                    Name = u.Name,
                    Password = u.Password ?? string.Empty,
                    Role = GatewayAdminRoles.Normalize(u.Role),
                    IsBuiltIn = true,
                });
            }

            // 文件账号补充
            var fileUsers = await _fileStore.ListAsync(ct).ConfigureAwait(false);
            foreach (var u in fileUsers)
            {
                if (string.IsNullOrWhiteSpace(u.Name)) continue;
                if (!seen.Add(u.Name))
                {
                    _logger?.LogWarning("users.json 中的账号 \"{Name}\" 与 appsettings 同名，已忽略文件项。", u.Name);
                    continue;
                }
                list.Add(new GatewayAdminUser
                {
                    Name = u.Name,
                    Password = u.Password ?? string.Empty,
                    Role = GatewayAdminRoles.Normalize(u.Role),
                    IsBuiltIn = false,
                });
            }

            return list;
        }

        public async Task<GatewayAdminUser?> FindAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var all = await ListAsync(ct).ConfigureAwait(false);
            return all.FirstOrDefault(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<GatewayAdminUser?> ValidateAsync(string name, string password, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(name) || password is null) return null;
            var u = await FindAsync(name, ct).ConfigureAwait(false);
            if (u is null) return null;
            var storedPassword = u.Password ?? string.Empty;
            if (!GatewayAdminPasswordHasher.Verify(storedPassword, password))
            {
                return null;
            }

            // 兼容旧版 users.json 明文密码：首次成功登录后透明升级为 PBKDF2。
            if (!u.IsBuiltIn && !GatewayAdminPasswordHasher.IsHashed(storedPassword))
            {
                await _fileStore.UpdateAsync(u.Name, password, null, ct).ConfigureAwait(false);
            }
            return u;
        }

        public async Task<int> CountAdminsAsync(CancellationToken ct)
        {
            var all = await ListAsync(ct).ConfigureAwait(false);
            return all.Count(u => string.Equals(u.Role, GatewayAdminRoles.Admin, StringComparison.OrdinalIgnoreCase));
        }
    }
}
