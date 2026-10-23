using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SyZero.Gateway;

namespace SyZero.Gateway.Admin.Services
{
    /// <summary>
    /// 默认的文件型 <see cref="IGatewayConfigurationStore"/> 实现：在 ContentRoot 下读写
    /// <c>configuration.json</c>，写入时自动备份并触发 <see cref="IGatewayConfigurationApplier"/>。
    /// 仅作为 SyZero.Gateway.Admin 的兜底；具体网关实现包通常会提供专属 Store。
    /// </summary>
    public sealed class FileGatewayConfigurationStore : IGatewayConfigurationStore
    {
        private const string ConfigFileName = "configuration.json";
        private const string BackupPattern = "configuration.backup.*.json";
        private const int DefaultMaxBackupCount = 10;

        private static readonly Regex BackupIdRegex =
            new Regex(@"^configuration\.backup\.(?<id>[^.]+)\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly string _configPath;
        private readonly IGatewayConfigurationApplier _applier;
        private readonly IConfigurationRoot? _configurationRoot;
        private readonly ILogger<FileGatewayConfigurationStore> _logger;
        private readonly int _maxBackupCount;

        public FileGatewayConfigurationStore(
            IWebHostEnvironment env,
            IConfiguration configuration,
            IGatewayConfigurationApplier applier,
            ILogger<FileGatewayConfigurationStore> logger)
        {
            _configPath = Path.Combine(env.ContentRootPath, ConfigFileName);
            _applier = applier;
            _configurationRoot = configuration as IConfigurationRoot;
            _logger = logger;
            _maxBackupCount = ReadMaxBackupCount(configuration);
        }

        private static int ReadMaxBackupCount(IConfiguration configuration)
        {
            var raw = configuration["Gateway:Admin:Backup:MaxCount"];
            if (int.TryParse(raw, out var v) && v > 0)
            {
                return v;
            }
            return DefaultMaxBackupCount;
        }

        public string ConfigPath => _configPath;

        public DateTime? LastBackupTime
        {
            get
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    return null;
                }

                var latest = Directory.EnumerateFiles(dir, "configuration.backup.*.json")
                    .Select(File.GetLastWriteTime)
                    .OrderByDescending(t => t)
                    .FirstOrDefault();

                return latest == default ? null : latest;
            }
        }

        public async Task<GatewayConfiguration> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_configPath))
            {
                return new GatewayConfiguration();
            }

            await using var stream = File.OpenRead(_configPath);
            var dto = await JsonSerializer.DeserializeAsync<GatewayConfiguration>(stream, ReadOptions, cancellationToken)
                      ?? new GatewayConfiguration();
            dto.Routes ??= new System.Collections.Generic.List<GatewayRoute>();
            dto.SwaggerEndPoints ??= new System.Collections.Generic.List<GatewaySwaggerEndPoint>();
            return dto;
        }

        public async Task WriteAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                BackupCurrent();

                var tempPath = _configPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await using (var stream = File.Create(tempPath))
                    {
                        await JsonSerializer.SerializeAsync(stream, configuration, WriteOptions, cancellationToken).ConfigureAwait(false);
                    }
                    File.Move(tempPath, _configPath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }

                CleanupBackups();

                await _applier.ApplyAsync(configuration, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            var dto = await ReadAsync(cancellationToken).ConfigureAwait(false);
            _configurationRoot?.Reload();
            await _applier.ApplyAsync(dto, cancellationToken).ConfigureAwait(false);
        }

        public Task<IReadOnlyList<GatewayConfigurationBackup>> ListBackupsAsync(CancellationToken cancellationToken = default)
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return Task.FromResult<IReadOnlyList<GatewayConfigurationBackup>>(Array.Empty<GatewayConfigurationBackup>());
            }

            var list = new List<GatewayConfigurationBackup>();
            foreach (var path in Directory.EnumerateFiles(dir, BackupPattern))
            {
                var name = Path.GetFileName(path);
                var match = BackupIdRegex.Match(name);
                if (!match.Success)
                {
                    continue;
                }

                var info = new FileInfo(path);
                list.Add(new GatewayConfigurationBackup
                {
                    Id = match.Groups["id"].Value,
                    CreatedAt = info.LastWriteTime,
                    SizeBytes = info.Length
                });
            }

            list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return Task.FromResult<IReadOnlyList<GatewayConfigurationBackup>>(list);
        }

        public async Task<GatewayConfiguration?> ReadBackupAsync(string id, CancellationToken cancellationToken = default)
        {
            var path = ResolveBackupPath(id);
            if (path is null || !File.Exists(path))
            {
                return null;
            }

            await using var stream = File.OpenRead(path);
            var dto = await JsonSerializer.DeserializeAsync<GatewayConfiguration>(stream, ReadOptions, cancellationToken).ConfigureAwait(false)
                      ?? new GatewayConfiguration();
            dto.Routes ??= new List<GatewayRoute>();
            dto.SwaggerEndPoints ??= new List<GatewaySwaggerEndPoint>();
            return dto;
        }

        public async Task RestoreBackupAsync(string id, CancellationToken cancellationToken = default)
        {
            var dto = await ReadBackupAsync(id, cancellationToken).ConfigureAwait(false)
                      ?? throw new FileNotFoundException($"未找到备份：{id}");
            await WriteAsync(dto, cancellationToken).ConfigureAwait(false);
        }

        public Task DeleteBackupAsync(string id, CancellationToken cancellationToken = default)
        {
            var path = ResolveBackupPath(id);
            if (path is null || !File.Exists(path))
            {
                throw new FileNotFoundException($"未找到备份：{id}");
            }
            File.Delete(path);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 公开的 BackupId → 路径解析；非法 id 返回 null。
        /// </summary>
        internal string? ResolveBackupPath(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || id.Contains('.') || id.Contains('/') || id.Contains('\\'))
            {
                return null;
            }
            var dir = Path.GetDirectoryName(_configPath);
            return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, $"configuration.backup.{id}.json");
        }

        private void BackupCurrent()
        {
            if (!File.Exists(_configPath))
            {
                return;
            }

            var dir = Path.GetDirectoryName(_configPath)!;
            // 精度到毫秒，避免短时间内连续两次备份名冲突（例如 restore 紧跟 write 场景）。
            var backupName = $"configuration.backup.{DateTime.Now:yyyyMMddHHmmssfff}.json";
            var backupPath = Path.Combine(dir, backupName);
            try
            {
                File.Copy(_configPath, backupPath, overwrite: true);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "备份网关配置文件失败：{BackupPath}", backupPath);
            }
        }

        private void CleanupBackups()
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return;
            }

            var backups = Directory.EnumerateFiles(dir, BackupPattern)
                .OrderByDescending(File.GetLastWriteTime)
                .Skip(_maxBackupCount)
                .ToList();

            foreach (var file in backups)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "清理备份文件失败：{File}", file);
                }
            }
        }
    }
}
