#nullable enable
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyZero.Gateway.Admin.Auditing;
using SyZero.Gateway.Admin.Events;

namespace SyZero.Gateway.Admin.Controllers
{
    /// <summary>
    /// 网关配置备份管理 API：列出 / 下载 / 还原 / 删除。
    /// 依赖 <see cref="IGatewayConfigurationStore"/> 中的备份扩展方法；
    /// 当具体实现未实现备份能力时，默认方法返回空列表或抛 <see cref="NotSupportedException"/>。
    /// </summary>
    [ApiController]
    [Route("admin-api/backups")]
    [IgnoreAntiforgeryToken]
    public sealed class BackupsController : ControllerBase
    {
        private static readonly JsonSerializerOptions DownloadOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IGatewayConfigurationStore _store;
        private readonly IGatewayAdminAuditLog _audit;
        private readonly IGatewayAdminEventBus _bus;

        public BackupsController(IGatewayConfigurationStore store, IGatewayAdminAuditLog audit, IGatewayAdminEventBus bus)
        {
            _store = store;
            _audit = audit;
            _bus = bus;
        }

        [HttpGet("")]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _store.ListBackupsAsync(ct);
            return Ok(list);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id, CancellationToken ct)
        {
            var dto = await _store.ReadBackupAsync(id, ct);
            if (dto is null)
            {
                return NotFound();
            }
            var json = JsonSerializer.Serialize(dto, DownloadOptions);
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"configuration.backup.{id}.json");
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore(string id, CancellationToken ct)
        {
            try
            {
                await _store.RestoreBackupAsync(id, ct);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return Problem(statusCode: 501, title: ex.Message);
            }
            await _audit.AppendAsync(new GatewayAdminAuditEntry
            {
                User = HttpContext.User?.Identity?.Name,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Action = "backup.restore",
                Target = id,
                Summary = $"还原备份 {id}"
            }, ct);
            _bus.Publish(new GatewayAdminEvent
            {
                Type = "backup.restore",
                User = HttpContext.User?.Identity?.Name,
                Target = id,
                Summary = $"还原备份 {id}",
            });
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            try
            {
                await _store.DeleteBackupAsync(id, ct);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (NotSupportedException ex)
            {
                return Problem(statusCode: 501, title: ex.Message);
            }
            await _audit.AppendAsync(new GatewayAdminAuditEntry
            {
                User = HttpContext.User?.Identity?.Name,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Action = "backup.delete",
                Target = id,
                Summary = $"删除备份 {id}"
            }, ct);
            _bus.Publish(new GatewayAdminEvent
            {
                Type = "backup.delete",
                User = HttpContext.User?.Identity?.Name,
                Target = id,
                Summary = $"删除备份 {id}",
            });
            return NoContent();
        }
    }
}
