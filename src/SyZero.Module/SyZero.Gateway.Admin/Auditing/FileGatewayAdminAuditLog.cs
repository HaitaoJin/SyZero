#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SyZero.Gateway.Admin.Auditing
{
    /// <summary>
    /// 文件型审计日志实现：以 JSONL（每行一个 JSON 对象，append-only）写入磁盘。
    /// 路径由 <c>Gateway:Admin:Audit:Path</c> 控制（默认 <c>&lt;ContentRoot&gt;/logs/gateway-admin-audit.jsonl</c>），
    /// 启用开关为 <c>Gateway:Admin:Audit:Enabled</c>（默认 true）。
    /// 不实现轮转/归档，建议宿主自行处理。
    /// </summary>
    public sealed class FileGatewayAdminAuditLog : IGatewayAdminAuditLog
    {
        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly string _path;
        private readonly bool _enabled;
        private readonly ILogger<FileGatewayAdminAuditLog> _logger;

        public FileGatewayAdminAuditLog(
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<FileGatewayAdminAuditLog> logger)
        {
            _logger = logger;
            _enabled = !string.Equals(configuration["Gateway:Admin:Audit:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
            var configured = configuration["Gateway:Admin:Audit:Path"];
            _path = !string.IsNullOrWhiteSpace(configured)
                ? Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured)
                : Path.Combine(env.ContentRootPath, "logs", "gateway-admin-audit.jsonl");
        }

        public bool Enabled => _enabled;

        public async Task AppendAsync(GatewayAdminAuditEntry entry, CancellationToken cancellationToken = default)
        {
            if (!_enabled || entry is null)
            {
                return;
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var line = JsonSerializer.Serialize(entry, WriteOptions);
                await File.AppendAllTextAsync(_path, line + Environment.NewLine, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "写入网关管理审计日志失败：{Path}", _path);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<IReadOnlyList<GatewayAdminAuditEntry>> ReadRecentAsync(int take, CancellationToken cancellationToken = default)
        {
            if (!_enabled || !File.Exists(_path) || take <= 0)
            {
                return Array.Empty<GatewayAdminAuditEntry>();
            }

            // 流式读取并只保留末尾 N 行，避免大型审计日志被一次性载入内存。
            var lines = new Queue<string>(Math.Min(take, 4096));
            await using (var stream = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 16 * 1024,
                useAsync: true))
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                {
                    if (lines.Count == take)
                    {
                        lines.Dequeue();
                    }
                    lines.Enqueue(line);
                }
            }

            var result = new List<GatewayAdminAuditEntry>(lines.Count);
            foreach (var line in lines.Reverse())
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                try
                {
                    var entry = JsonSerializer.Deserialize<GatewayAdminAuditEntry>(line);
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
                catch (JsonException)
                {
                    // 忽略损坏的行
                }
            }
            return result;
        }
    }
}
