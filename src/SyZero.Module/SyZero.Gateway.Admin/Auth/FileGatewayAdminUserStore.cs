#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 文件型管理台账号存储，路径为 <c>&lt;ContentRoot&gt;/users.json</c>，
    /// 可由配置项 <c>Gateway:Admin:UsersFile</c> 覆盖（绝对或相对路径）。
    /// 使用进程锁 + 写临时文件 + replace 实现简单的原子写入。
    /// </summary>
    public sealed class FileGatewayAdminUserStore : IGatewayAdminUserStore
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };

        private readonly string _path;
        private readonly ILogger<FileGatewayAdminUserStore>? _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public FileGatewayAdminUserStore(IHostEnvironment env, IConfiguration cfg, ILogger<FileGatewayAdminUserStore>? logger = null)
        {
            _logger = logger;
            var configured = cfg["Gateway:Admin:UsersFile"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                _path = Path.Combine(env.ContentRootPath, "users.json");
            }
            else
            {
                _path = Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(env.ContentRootPath, configured);
            }
        }

        /// <summary>测试用构造（直接指定路径）。</summary>
        internal FileGatewayAdminUserStore(string filePath)
        {
            _path = filePath;
        }

        /// <summary>暴露文件路径供诊断或测试使用。</summary>
        public string FilePath => _path;

        public async Task<IReadOnlyList<GatewayAdminUser>> ListAsync(CancellationToken ct)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return ReadAllNoLock();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<GatewayAdminUser?> FindAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = await ListAsync(ct).ConfigureAwait(false);
            return all.FirstOrDefault(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> AddAsync(GatewayAdminUser user, CancellationToken ct)
        {
            if (user is null || string.IsNullOrWhiteSpace(user.Name)) return false;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var list = ReadAllNoLock();
                if (list.Any(u => string.Equals(u.Name, user.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                list.Add(new GatewayAdminUser
                {
                    Name = user.Name,
                    Password = GatewayAdminPasswordHasher.Hash(user.Password ?? string.Empty),
                    Role = GatewayAdminRoles.Normalize(user.Role),
                });
                WriteAllNoLock(list);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> UpdateAsync(string name, string? newPassword, string? newRole, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var list = ReadAllNoLock();
                var idx = list.FindIndex(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                if (newPassword is not null) list[idx].Password = GatewayAdminPasswordHasher.Hash(newPassword);
                if (newRole is not null) list[idx].Role = GatewayAdminRoles.Normalize(newRole);
                WriteAllNoLock(list);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> RemoveAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var list = ReadAllNoLock();
                var idx = list.FindIndex(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                list.RemoveAt(idx);
                WriteAllNoLock(list);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        // ------------ internals ------------

        private List<GatewayAdminUser> ReadAllNoLock()
        {
            if (!File.Exists(_path))
            {
                return new List<GatewayAdminUser>();
            }
            try
            {
                var raw = File.ReadAllText(_path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(raw)) return new List<GatewayAdminUser>();
                var parsed = JsonSerializer.Deserialize<List<GatewayAdminUser>>(raw, ReadOptions);
                return parsed ?? new List<GatewayAdminUser>();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "无法解析 users.json，使用空列表代替；文件路径：{Path}", _path);
                return new List<GatewayAdminUser>();
            }
        }

        private void WriteAllNoLock(IReadOnlyList<GatewayAdminUser> users)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var json = JsonSerializer.Serialize(users, WriteOptions);
            try
            {
                File.WriteAllText(tmp, json, new UTF8Encoding(false));
                // 临时文件与 users.json 位于同一目录，替换不会跨卷。
                File.Move(tmp, _path, overwrite: true);
            }
            finally
            {
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }
            }
        }
    }
}
