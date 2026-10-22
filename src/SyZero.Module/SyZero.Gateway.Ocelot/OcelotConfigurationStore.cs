using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SyZero.Gateway;

namespace SyZero.Gateway.Ocelot
{
    /// <summary>
    /// 基于 Ocelot 的 <see cref="IGatewayConfigurationStore"/> 实现：
    /// 在内容根目录读写 <c>configuration.json</c>，写入时自动备份并触发 <see cref="IGatewayConfigurationApplier"/> 热下发。
    /// </summary>
    public sealed class OcelotConfigurationStore : IGatewayConfigurationStore
    {
        private const string ConfigFileName = "configuration.json";
        private const int MaxBackupCount = 10;

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly string _configPath;
        private readonly IGatewayConfigurationApplier _applier;
        private readonly IConfigurationRoot? _configurationRoot;
        private readonly ILogger<OcelotConfigurationStore> _logger;

        public OcelotConfigurationStore(
            IWebHostEnvironment env,
            IConfiguration configuration,
            IGatewayConfigurationApplier applier,
            ILogger<OcelotConfigurationStore> logger)
        {
            _configPath = Path.Combine(env.ContentRootPath, ConfigFileName);
            _applier = applier;
            _configurationRoot = configuration as IConfigurationRoot;
            _logger = logger;
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
            dto.Routes ??= new();
            dto.SwaggerEndPoints ??= new();
            return dto;
        }

        public async Task WriteAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                BackupCurrent();

                var tempPath = _configPath + ".tmp";
                await using (var stream = File.Create(tempPath))
                {
                    await JsonSerializer.SerializeAsync(stream, configuration, WriteOptions, cancellationToken).ConfigureAwait(false);
                }
                File.Move(tempPath, _configPath, overwrite: true);

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

        private void BackupCurrent()
        {
            if (!File.Exists(_configPath))
            {
                return;
            }

            var dir = Path.GetDirectoryName(_configPath)!;
            var backupName = $"configuration.backup.{DateTime.Now:yyyyMMddHHmmss}.json";
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

            var backups = Directory.EnumerateFiles(dir, "configuration.backup.*.json")
                .OrderByDescending(File.GetLastWriteTime)
                .Skip(MaxBackupCount)
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
