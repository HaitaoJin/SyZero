#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SyZero.Gateway.Admin.Auditing;
using SyZero.Gateway.Admin.Events;
using SyZero.Service;

namespace SyZero.Gateway.Admin.Controllers
{
    /// <summary>
    /// 网关管理 API：对网关配置中的 Routes 与 SwaggerEndPoints 做完整 CRUD，
    /// 以及概览/重载/全量配置导入导出/运行时 Diff/路由 reorder/克隆/服务发现下拉/审计读取等扩展端点。
    /// 配套 Razor Pages UI（/admin）。
    /// </summary>
    [ApiController]
    [Route("admin-api")]
    [IgnoreAntiforgeryToken]
    public sealed partial class AdminController : ControllerBase
    {
        private static readonly JsonSerializerOptions DownloadOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly IGatewayConfigurationStore _store;
        private readonly IGatewayAdminAuditLog _audit;
        private readonly IGatewayAdminEventBus _bus;
        private readonly IServiceProvider _services;
        private readonly string? _currentServiceName;

        public AdminController(
            IGatewayConfigurationStore store,
            IGatewayAdminAuditLog audit,
            IGatewayAdminEventBus bus,
            IServiceProvider services,
            IConfiguration configuration)
        {
            _store = store;
            _audit = audit;
            _bus = bus;
            _services = services;
            _currentServiceName = configuration["SyZero:Name"];
        }

        // ---------- 健康/概览 ----------

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            return Ok(new
            {
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),
                ConfigPath = _store.ConfigPath,
                LastBackupTime = _store.LastBackupTime,
                RouteCount = dto.Routes.Count,
                SwaggerEndPointCount = dto.SwaggerEndPoints.Count,
                AuditEnabled = _audit.Enabled
            });
        }

        [HttpPost("reload")]
        public async Task<IActionResult> Reload(CancellationToken ct)
        {
            await _store.ReloadAsync(ct);
            await Audit("config.reload", null, "强制重载配置", ct);
            return NoContent();
        }

        // ---------- 全量配置（导入 / 导出 / 原生 JSON 编辑） ----------

        [HttpGet("configuration")]
        public async Task<IActionResult> GetConfiguration(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            var json = JsonSerializer.Serialize(dto, DownloadOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            // 同时支持「下载附件」与「在线编辑器读取」：通过查询参数 download=1 提示浏览器保存。
            if (HttpContext.Request.Query.TryGetValue("download", out var d) && d == "1")
            {
                return File(bytes, "application/json", "configuration.json");
            }
            return Content(json, "application/json", Encoding.UTF8);
        }

        [HttpPut("configuration")]
        public async Task<IActionResult> ReplaceConfiguration([FromBody] JsonElement payload, CancellationToken ct)
        {
            GatewayConfiguration? dto;
            try
            {
                dto = payload.Deserialize<GatewayConfiguration>(ReadOptions);
            }
            catch (JsonException ex)
            {
                return Problem(statusCode: 400, title: "JSON 解析失败：" + ex.Message);
            }
            if (dto is null)
            {
                return Problem(statusCode: 400, title: "请求体不能为空。");
            }
            dto.Routes ??= new List<GatewayRoute>();
            dto.SwaggerEndPoints ??= new List<GatewaySwaggerEndPoint>();

            await _store.WriteAsync(dto, ct);
            await Audit("config.replace", null,
                $"全量替换：{dto.Routes.Count} 条路由 / {dto.SwaggerEndPoints.Count} 个 Swagger 端点", ct);
            return NoContent();
        }

        // ---------- 运行时信息 + Diff ----------

        [HttpGet("runtime")]
        public async Task<IActionResult> GetRuntime(CancellationToken ct)
        {
            var stored = await _store.ReadAsync(ct);
            var storedJson = JsonSerializer.Serialize(stored, DownloadOptions);

            var gateway = _services.GetService(typeof(IGateway)) as IGateway;
            string? providerName = gateway?.ProviderName;
            string? runtimeJson = null;
            int? runtimeRouteCount = null;
            if (gateway is not null)
            {
                try
                {
                    var runtime = await gateway.GetConfigurationAsync(ct);
                    runtimeJson = JsonSerializer.Serialize(runtime, DownloadOptions);
                    runtimeRouteCount = runtime.Routes.Count;
                }
                catch (Exception ex)
                {
                    return Ok(new
                    {
                        ProviderName = providerName,
                        Available = false,
                        Error = ex.Message
                    });
                }
            }

            var storedHash = Sha256(storedJson);
            var runtimeHash = runtimeJson is null ? null : Sha256(runtimeJson);
            return Ok(new
            {
                ProviderName = providerName,
                Available = gateway is not null,
                StoredHash = storedHash,
                RuntimeHash = runtimeHash,
                RouteCountStored = stored.Routes.Count,
                RouteCountRuntime = runtimeRouteCount,
                Diverged = runtimeHash is not null && storedHash != runtimeHash
            });
        }

        [HttpGet("runtime/diff")]
        public async Task<IActionResult> GetRuntimeDiff(CancellationToken ct)
        {
            var stored = await _store.ReadAsync(ct);
            var storedJson = JsonSerializer.Serialize(stored, DownloadOptions);
            string? runtimeJson = null;
            if (_services.GetService(typeof(IGateway)) is IGateway gateway)
            {
                try
                {
                    var runtime = await gateway.GetConfigurationAsync(ct);
                    runtimeJson = JsonSerializer.Serialize(runtime, DownloadOptions);
                }
                catch
                {
                    runtimeJson = null;
                }
            }

            return Ok(new { Stored = storedJson, Runtime = runtimeJson });
        }

        // ---------- Routes ----------

        [HttpGet("routes")]
        public async Task<ActionResult<List<GatewayRoute>>> GetRoutes(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            return dto.Routes;
        }

        [HttpPost("routes")]
        public async Task<IActionResult> CreateRoute([FromBody] GatewayRoute route, CancellationToken ct)
        {
            if (route is null || string.IsNullOrWhiteSpace(route.UpstreamPathTemplate))
            {
                return Problem(statusCode: 400, title: "UpstreamPathTemplate 不能为空。");
            }

            var dto = await _store.ReadAsync(ct);
            if (HasConflict(dto.Routes, route, ignoreIndex: -1))
            {
                return Problem(statusCode: 409, title: "已存在相同 UpstreamPathTemplate + HTTP 方法的路由。");
            }

            dto.Routes.Add(route);
            await _store.WriteAsync(dto, ct);
            await Audit("route.create", route.UpstreamPathTemplate, $"新增路由 {route.UpstreamPathTemplate}", ct);
            return CreatedAtAction(nameof(GetRoutes), new { index = dto.Routes.Count - 1 }, route);
        }

        [HttpPut("routes/{index:int}")]
        public async Task<IActionResult> UpdateRoute(int index, [FromBody] GatewayRoute route, CancellationToken ct)
        {
            if (route is null || string.IsNullOrWhiteSpace(route.UpstreamPathTemplate))
            {
                return Problem(statusCode: 400, title: "UpstreamPathTemplate 不能为空。");
            }

            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count)
            {
                return NotFound();
            }
            if (HasConflict(dto.Routes, route, ignoreIndex: index))
            {
                return Problem(statusCode: 409, title: "已存在相同 UpstreamPathTemplate + HTTP 方法的路由。");
            }

            dto.Routes[index] = route;
            await _store.WriteAsync(dto, ct);
            await Audit("route.update", index.ToString(), $"更新路由 #{index} → {route.UpstreamPathTemplate}", ct);
            return NoContent();
        }

        [HttpDelete("routes/{index:int}")]
        public async Task<IActionResult> DeleteRoute(int index, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count)
            {
                return NotFound();
            }
            var removed = dto.Routes[index].UpstreamPathTemplate;
            dto.Routes.RemoveAt(index);
            await _store.WriteAsync(dto, ct);
            await Audit("route.delete", index.ToString(), $"删除路由 #{index} ({removed})", ct);
            return NoContent();
        }

        public sealed class MoveRequest
        {
            public string Direction { get; set; } = "down";
        }

        public sealed class BulkToggleRequest
        {
            public List<int> Indexes { get; set; } = new List<int>();
            public bool Disabled { get; set; }
        }

        public sealed class BulkTagsRequest
        {
            public List<int> Indexes { get; set; } = new List<int>();
            public List<string>? Tags { get; set; }
            /// <summary>set | add | remove，默认 set。</summary>
            public string? Op { get; set; }
        }

        public sealed class ReorderRequest
        {
            /// <summary>新顺序——元素是当前 Routes 列表中的索引，长度必须与现有路由数一致。</summary>
            public List<int> Order { get; set; } = new List<int>();
        }

        [HttpPost("routes/{index:int}/move")]
        public async Task<IActionResult> MoveRoute(int index, [FromBody] MoveRequest body, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count)
            {
                return NotFound();
            }
            var down = !string.Equals(body?.Direction, "up", StringComparison.OrdinalIgnoreCase);
            var target = down ? index + 1 : index - 1;
            if (target < 0 || target >= dto.Routes.Count)
            {
                return Problem(statusCode: 400, title: "已到达边界，无法继续移动。");
            }
            (dto.Routes[index], dto.Routes[target]) = (dto.Routes[target], dto.Routes[index]);
            await _store.WriteAsync(dto, ct);
            await Audit("route.move", index.ToString(), $"路由 #{index} ↔ #{target} ({(down ? "下移" : "上移")})", ct);
            return NoContent();
        }

        [HttpPost("routes/{index:int}/clone")]
        public async Task<IActionResult> CloneRoute(int index, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count)
            {
                return NotFound();
            }

            var src = dto.Routes[index];
            // 通过 JSON round-trip 做深拷贝，避免共享子对象。
            var json = JsonSerializer.Serialize(src, DownloadOptions);
            var copy = JsonSerializer.Deserialize<GatewayRoute>(json, ReadOptions)!;

            // 自动追加 -copy 直到不冲突。
            var baseTemplate = copy.UpstreamPathTemplate;
            var attempt = 1;
            while (HasConflict(dto.Routes, copy, ignoreIndex: -1))
            {
                copy.UpstreamPathTemplate = attempt == 1
                    ? $"{baseTemplate}-copy"
                    : $"{baseTemplate}-copy{attempt}";
                attempt++;
                if (attempt > 50)
                {
                    return Problem(statusCode: 409, title: "克隆失败：连续 50 次仍命中冲突。");
                }
            }

            dto.Routes.Insert(index + 1, copy);
            await _store.WriteAsync(dto, ct);
            await Audit("route.clone", index.ToString(), $"克隆路由 #{index} → {copy.UpstreamPathTemplate}", ct);
            return Ok(new { Index = index + 1, Route = copy });
        }

        // 批量启停：按下标列表 + 目标状态。
        [HttpPost("routes/bulk-toggle")]
        public async Task<IActionResult> BulkToggleRoutes([FromBody] BulkToggleRequest request, CancellationToken ct)
        {
            if (request is null || request.Indexes is null || request.Indexes.Count == 0)
            {
                return Problem(statusCode: 400, title: "Indexes 不能为空。");
            }
            var dto = await _store.ReadAsync(ct);
            var changed = 0;
            var ignored = new List<int>();
            foreach (var i in request.Indexes.Distinct())
            {
                if (i < 0 || i >= dto.Routes.Count) { ignored.Add(i); continue; }
                if (dto.Routes[i].Disabled != request.Disabled)
                {
                    dto.Routes[i].Disabled = request.Disabled;
                    changed++;
                }
            }
            if (changed > 0)
            {
                await _store.WriteAsync(dto, ct);
                await Audit("route.bulk-toggle", string.Join(",", request.Indexes),
                    $"{(request.Disabled ? "禁用" : "启用")} {changed} 条路由", ct);
            }
            return Ok(new { Changed = changed, Ignored = ignored });
        }

        // 标签批量更新：覆盖 / 追加 / 移除。
        [HttpPost("routes/bulk-tags")]
        public async Task<IActionResult> BulkUpdateTags([FromBody] BulkTagsRequest request, CancellationToken ct)
        {
            if (request is null || request.Indexes is null || request.Indexes.Count == 0)
            {
                return Problem(statusCode: 400, title: "Indexes 不能为空。");
            }
            var op = (request.Op ?? "set").ToLowerInvariant();
            if (op != "set" && op != "add" && op != "remove")
            {
                return Problem(statusCode: 400, title: "Op 仅支持 set / add / remove。");
            }
            var tags = request.Tags ?? new List<string>();
            var dto = await _store.ReadAsync(ct);
            var changed = 0;
            foreach (var i in request.Indexes.Distinct())
            {
                if (i < 0 || i >= dto.Routes.Count) continue;
                var route = dto.Routes[i];
                var current = route.Tags ?? new List<string>();
                List<string> next;
                if (op == "set")
                {
                    next = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
                else if (op == "add")
                {
                    next = current.Concat(tags).Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
                else
                {
                    next = current.Where(t => !tags.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
                }
                route.Tags = next.Count == 0 ? null : next;
                changed++;
            }
            if (changed > 0)
            {
                await _store.WriteAsync(dto, ct);
                await Audit("route.bulk-tags", string.Join(",", request.Indexes),
                    $"{op}: {string.Join("|", tags)} → {changed} 条路由", ct);
            }
            return Ok(new { Changed = changed });
        }

        // 全部已用标签（升序）。
        [HttpGet("routes/tags")]
        public async Task<IActionResult> GetAllTags(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            var all = dto.Routes
                .Where(r => r.Tags is { Count: > 0 })
                .SelectMany(r => r.Tags!)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Ok(all);
        }

        // 自由拖拽重排：order 是当前 Routes 索引的一个排列。
        [HttpPost("routes/reorder")]
        public async Task<IActionResult> ReorderRoutes([FromBody] ReorderRequest request, CancellationToken ct)
        {
            if (request?.Order is null || request.Order.Count == 0)
            {
                return BadRequest("Order 不能为空");
            }
            var dto = await _store.ReadAsync(ct);
            var n = dto.Routes.Count;
            if (request.Order.Count != n)
            {
                return BadRequest($"Order 长度 {request.Order.Count} 与当前路由数 {n} 不一致");
            }
            var seen = new bool[n];
            foreach (var i in request.Order)
            {
                if (i < 0 || i >= n || seen[i])
                {
                    return BadRequest("Order 必须是 0..n-1 的排列");
                }
                seen[i] = true;
            }
            var reordered = new List<GatewayRoute>(n);
            foreach (var i in request.Order)
            {
                reordered.Add(dto.Routes[i]);
            }
            dto.Routes = reordered;
            await _store.WriteAsync(dto, ct);
            await Audit("route.reorder", null, $"重排 {n} 条路由", ct);
            return NoContent();
        }

        // 路由导出：?format=yaml|json，默认 json。
        [HttpGet("routes/export")]
        public async Task<IActionResult> ExportRoutes([FromQuery] string? format, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            var fmt = (format ?? "json").Trim().ToLowerInvariant();
            if (fmt == "yaml" || fmt == "yml")
            {
                var json = JsonSerializer.Serialize(dto.Routes, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });
                using var doc = JsonDocument.Parse(json);
                var node = JsonElementToObject(doc.RootElement);
                var ser = new YamlDotNet.Serialization.SerializerBuilder()
                    .ConfigureDefaultValuesHandling(YamlDotNet.Serialization.DefaultValuesHandling.OmitNull)
                    .Build();
                var yaml = ser.Serialize(node);
                Response.Headers["Content-Disposition"] = "attachment; filename=routes.yaml";
                return Content(yaml, "application/yaml");
            }
            else
            {
                var json = JsonSerializer.Serialize(dto.Routes, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });
                Response.Headers["Content-Disposition"] = "attachment; filename=routes.json";
                return Content(json, "application/json");
            }
        }

        // 路由导入：query format=yaml|json（默认按 body 首字符自动判断）；mode=replace|append（默认 append）。
        [HttpPost("routes/import")]
        public async Task<IActionResult> ImportRoutes(
            [FromQuery] string? format,
            [FromQuery] string? mode,
            CancellationToken ct)
        {
            string body;
            using (var sr = new System.IO.StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await sr.ReadToEndAsync();
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest("Body 为空");
            }

            var fmt = (format ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(fmt))
            {
                fmt = body.TrimStart().StartsWith("[") || body.TrimStart().StartsWith("{") ? "json" : "yaml";
            }

            string json;
            try
            {
                if (fmt == "yaml" || fmt == "yml")
                {
                    var deser = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                    var obj = deser.Deserialize<object?>(body);
                    json = JsonSerializer.Serialize(obj);
                }
                else
                {
                    json = body;
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"解析失败：{ex.Message}");
            }

            List<GatewayRoute>? incoming;
            try
            {
                incoming = JsonSerializer.Deserialize<List<GatewayRoute>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"反序列化为路由列表失败：{ex.Message}");
            }
            if (incoming is null || incoming.Count == 0)
            {
                return BadRequest("未解析到任何路由");
            }

            var dto = await _store.ReadAsync(ct);
            var m = (mode ?? "append").Trim().ToLowerInvariant();
            if (m == "replace")
            {
                dto.Routes = incoming;
            }
            else
            {
                dto.Routes.AddRange(incoming);
            }
            await _store.WriteAsync(dto, ct);
            await Audit("route.import", $"format={fmt};mode={m}", $"导入 {incoming.Count} 条路由", ct);
            return Ok(new { Imported = incoming.Count, Mode = m, Format = fmt, Total = dto.Routes.Count });
        }

        private static object? JsonElementToObject(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in el.EnumerateObject())
                    {
                        dict[p.Name] = JsonElementToObject(p.Value);
                    }
                    return dict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in el.EnumerateArray())
                    {
                        list.Add(JsonElementToObject(item));
                    }
                    return list;
                case JsonValueKind.String:
                    return el.GetString();
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) return l;
                    return el.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }

        // ---------- Swagger Endpoints ----------

        [HttpGet("swagger-endpoints")]
        public async Task<ActionResult<List<GatewaySwaggerEndPoint>>> GetSwaggerEndPoints(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            return dto.SwaggerEndPoints;
        }

        [HttpPost("swagger-endpoints")]
        public async Task<IActionResult> CreateSwaggerEndPoint([FromBody] GatewaySwaggerEndPoint endpoint, CancellationToken ct)
        {
            if (endpoint is null || string.IsNullOrWhiteSpace(endpoint.Key))
            {
                return Problem(statusCode: 400, title: "Key 不能为空。");
            }

            var dto = await _store.ReadAsync(ct);
            if (dto.SwaggerEndPoints.Any(e => string.Equals(e.Key, endpoint.Key, StringComparison.OrdinalIgnoreCase)))
            {
                return Problem(statusCode: 409, title: $"Swagger 端点 '{endpoint.Key}' 已存在。");
            }

            dto.SwaggerEndPoints.Add(endpoint);
            await _store.WriteAsync(dto, ct);
            await Audit("swagger.create", endpoint.Key, $"新增 Swagger 端点 {endpoint.Key}", ct);
            return CreatedAtAction(nameof(GetSwaggerEndPoints), new { key = endpoint.Key }, endpoint);
        }

        [HttpPut("swagger-endpoints/{key}")]
        public async Task<IActionResult> UpdateSwaggerEndPoint(string key, [FromBody] GatewaySwaggerEndPoint endpoint, CancellationToken ct)
        {
            if (endpoint is null || string.IsNullOrWhiteSpace(endpoint.Key))
            {
                return Problem(statusCode: 400, title: "Key 不能为空。");
            }

            var dto = await _store.ReadAsync(ct);
            var existing = dto.SwaggerEndPoints.FindIndex(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
            if (existing < 0)
            {
                return NotFound();
            }

            // 如果改了 key，校验冲突
            if (!string.Equals(endpoint.Key, key, StringComparison.OrdinalIgnoreCase)
                && dto.SwaggerEndPoints.Any(e => string.Equals(e.Key, endpoint.Key, StringComparison.OrdinalIgnoreCase)))
            {
                return Problem(statusCode: 409, title: $"Swagger 端点 '{endpoint.Key}' 已存在。");
            }

            dto.SwaggerEndPoints[existing] = endpoint;
            await _store.WriteAsync(dto, ct);
            await Audit("swagger.update", key, $"更新 Swagger 端点 {key} → {endpoint.Key}", ct);
            return NoContent();
        }

        [HttpDelete("swagger-endpoints/{key}")]
        public async Task<IActionResult> DeleteSwaggerEndPoint(string key, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            var existing = dto.SwaggerEndPoints.FindIndex(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
            if (existing < 0)
            {
                return NotFound();
            }
            dto.SwaggerEndPoints.RemoveAt(existing);
            await _store.WriteAsync(dto, ct);
            await Audit("swagger.delete", key, $"删除 Swagger 端点 {key}", ct);
            return NoContent();
        }

        // Swagger 端点导出：?format=yaml|json，默认 json。
        [HttpGet("swagger-endpoints/export")]
        public async Task<IActionResult> ExportSwaggerEndPoints([FromQuery] string? format, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            var fmt = (format ?? "json").Trim().ToLowerInvariant();
            if (fmt == "yaml" || fmt == "yml")
            {
                var json = JsonSerializer.Serialize(dto.SwaggerEndPoints, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });
                using var doc = JsonDocument.Parse(json);
                var node = JsonElementToObject(doc.RootElement);
                var ser = new YamlDotNet.Serialization.SerializerBuilder()
                    .ConfigureDefaultValuesHandling(YamlDotNet.Serialization.DefaultValuesHandling.OmitNull)
                    .Build();
                var yaml = ser.Serialize(node);
                Response.Headers["Content-Disposition"] = "attachment; filename=swagger-endpoints.yaml";
                return Content(yaml, "application/yaml");
            }
            else
            {
                var json = JsonSerializer.Serialize(dto.SwaggerEndPoints, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });
                Response.Headers["Content-Disposition"] = "attachment; filename=swagger-endpoints.json";
                return Content(json, "application/json");
            }
        }

        // Swagger 端点导入：query format=yaml|json（默认按 body 首字符自动判断）；
        // mode=replace|append|merge（默认 append）；merge 时按 Key 去重，新值覆盖旧值。
        [HttpPost("swagger-endpoints/import")]
        public async Task<IActionResult> ImportSwaggerEndPoints(
            [FromQuery] string? format,
            [FromQuery] string? mode,
            CancellationToken ct)
        {
            string body;
            using (var sr = new System.IO.StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await sr.ReadToEndAsync();
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest("Body 为空");
            }

            var fmt = (format ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(fmt))
            {
                fmt = body.TrimStart().StartsWith("[") || body.TrimStart().StartsWith("{") ? "json" : "yaml";
            }

            string json;
            try
            {
                if (fmt == "yaml" || fmt == "yml")
                {
                    var deser = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                    var obj = deser.Deserialize<object?>(body);
                    json = JsonSerializer.Serialize(obj);
                }
                else
                {
                    json = body;
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"解析失败：{ex.Message}");
            }

            List<GatewaySwaggerEndPoint>? incoming;
            try
            {
                incoming = JsonSerializer.Deserialize<List<GatewaySwaggerEndPoint>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"反序列化为 Swagger 端点列表失败：{ex.Message}");
            }
            if (incoming is null || incoming.Count == 0)
            {
                return BadRequest("未解析到任何 Swagger 端点");
            }
            // 校验：每条都要有非空 Key。
            for (var i = 0; i < incoming.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(incoming[i]?.Key))
                {
                    return BadRequest($"第 {i} 条 Swagger 端点缺少 Key");
                }
            }

            var dto = await _store.ReadAsync(ct);
            var m = (mode ?? "append").Trim().ToLowerInvariant();
            int added = 0, replaced = 0;
            if (m == "replace")
            {
                dto.SwaggerEndPoints = incoming;
                added = incoming.Count;
            }
            else if (m == "merge")
            {
                foreach (var item in incoming)
                {
                    var idx = dto.SwaggerEndPoints.FindIndex(e => string.Equals(e.Key, item.Key, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        dto.SwaggerEndPoints[idx] = item;
                        replaced++;
                    }
                    else
                    {
                        dto.SwaggerEndPoints.Add(item);
                        added++;
                    }
                }
            }
            else
            {
                // append：拒绝与现有 Key 冲突。
                var existingKeys = new HashSet<string>(
                    dto.SwaggerEndPoints.Select(e => e.Key ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase);
                foreach (var item in incoming)
                {
                    if (!existingKeys.Add(item.Key!))
                    {
                        return BadRequest($"Key 冲突：{item.Key}（请使用 mode=merge 或 replace）");
                    }
                }
                dto.SwaggerEndPoints.AddRange(incoming);
                added = incoming.Count;
                m = "append";
            }
            await _store.WriteAsync(dto, ct);
            await Audit("swagger.import", $"format={fmt};mode={m}", $"导入 Swagger 端点：新增 {added} / 替换 {replaced}", ct);
            return Ok(new { Added = added, Replaced = replaced, Mode = m, Format = fmt, Total = dto.SwaggerEndPoints.Count });
        }

        // ---------- 服务发现下拉 ----------

        [HttpGet("services")]
        public async Task<IActionResult> GetServices(CancellationToken ct)
        {
            var sm = _services.GetService(typeof(IServiceManagement)) as IServiceManagement;
            if (sm is null)
            {
                return Ok(new { Available = false, Items = Array.Empty<string>() });
            }
            try
            {
                var names = await sm.GetAllServices();
                var items = (names ?? new List<string>())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return Ok(new { Available = true, Items = items });
            }
            catch (Exception ex)
            {
                return Ok(new { Available = false, Items = Array.Empty<string>(), Error = ex.Message });
            }
        }

        public sealed class RegisteredServiceInstanceDto
        {
            public string ServiceId { get; set; } = string.Empty;
            public string ServiceAddress { get; set; } = string.Empty;
            public int ServicePort { get; set; }
            public string Protocol { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public bool IsHealthy { get; set; }
            public bool Enabled { get; set; }
            public string HealthCheckUrl { get; set; } = string.Empty;
        }

        public sealed class RegisteredServiceInterfacesDto
        {
            public string ServiceName { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public int InstanceCount { get; set; }
            public int HealthyInstanceCount { get; set; }
            public int EndpointCount { get; set; }
            public string? SwaggerJsonUrl { get; set; }
            public string? SwaggerUiUrl { get; set; }
            public List<ServiceApiEndpointInfo> Endpoints { get; set; } = new List<ServiceApiEndpointInfo>();
            public List<RegisteredServiceInstanceDto> Instances { get; set; } = new List<RegisteredServiceInstanceDto>();
        }

        [HttpGet("service-interfaces")]
        public async Task<IActionResult> GetServiceInterfaces([FromQuery] string? service, CancellationToken ct)
        {
            var sm = _services.GetService(typeof(IServiceManagement)) as IServiceManagement;
            if (sm is null)
            {
                return Ok(new { Available = false, Items = Array.Empty<RegisteredServiceInterfacesDto>() });
            }
            try
            {
                var names = await sm.GetAllServices();
                var items = new List<RegisteredServiceInterfacesDto>();
                foreach (var name in (names ?? new List<string>())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(_currentServiceName)
                        && string.Equals(name, _currentServiceName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(service)
                        && !string.Equals(name, service, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ct.ThrowIfCancellationRequested();
                    var instances = (await sm.GetService(name))
                        .GroupBy(BuildInstanceKey, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group
                            .OrderByDescending(HasApiEndpointMetadata)
                            .ThenByDescending(s => s.IsHealthy && s.Enabled)
                            .ThenBy(s => s.ServiceID, StringComparer.OrdinalIgnoreCase)
                            .First())
                        .ToList();
                    if (instances == null || instances.Count == 0)
                    {
                        continue;
                    }

                    var primary = instances
                        .OrderByDescending(HasApiEndpointMetadata)
                        .ThenByDescending(s => s.IsHealthy && s.Enabled)
                        .ThenBy(s => s.ServiceID, StringComparer.OrdinalIgnoreCase)
                        .First();

                    var endpoints = instances
                        .SelectMany(ParseApiEndpoints)
                        .GroupBy(endpoint => endpoint.Method + "::" + endpoint.Path, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .OrderBy(endpoint => endpoint.Path, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(endpoint => endpoint.Method, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    items.Add(new RegisteredServiceInterfacesDto
                    {
                        ServiceName = name,
                        Version = primary.Version ?? string.Empty,
                        InstanceCount = instances.Count,
                        HealthyInstanceCount = instances.Count(s => s.IsHealthy && s.Enabled),
                        EndpointCount = endpoints.Count,
                        SwaggerJsonUrl = BuildServiceUrl(primary, "/openapi/v1.json"),
                        SwaggerUiUrl = null,
                        Endpoints = endpoints,
                        Instances = instances
                            .OrderByDescending(s => s.IsHealthy && s.Enabled)
                            .ThenBy(s => s.ServiceID, StringComparer.OrdinalIgnoreCase)
                            .Select(instance => new RegisteredServiceInstanceDto
                            {
                                ServiceId = instance.ServiceID,
                                ServiceAddress = instance.ServiceAddress,
                                ServicePort = instance.ServicePort,
                                Protocol = instance.ServiceProtocol.ToString().ToLowerInvariant(),
                                Version = instance.Version ?? string.Empty,
                                IsHealthy = instance.IsHealthy,
                                Enabled = instance.Enabled,
                                HealthCheckUrl = instance.HealthCheckUrl ?? string.Empty
                            })
                            .ToList()
                    });
                }
                return Ok(new { Available = true, Items = items });
            }
            catch (Exception ex)
            {
                return Ok(new { Available = false, Items = Array.Empty<RegisteredServiceInterfacesDto>(), Error = ex.Message });
            }
        }

        // ---------- 审计 ----------

        [HttpGet("audit")]
        public async Task<IActionResult> GetAudit(
            [FromQuery] int take,
            [FromQuery] string? user,
            [FromQuery] string? action,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct)
        {
            if (take <= 0) take = 200;
            if (take > 5000) take = 5000;
            // 多取一些以便过滤后仍有足够结果。
            var pre = Math.Min(Math.Max(take * 5, 1000), 20000);
            var entries = await _audit.ReadRecentAsync(pre, ct);
            var filtered = ApplyAuditFilter(entries, user, action, from, to);
            var items = filtered.Take(take).ToList();
            return Ok(new
            {
                Enabled = _audit.Enabled,
                Items = items,
                FilteredCount = filtered.Count,
                ScannedCount = entries.Count,
                Truncated = filtered.Count > take,
            });
        }

        [HttpGet("audit/export.csv")]
        public async Task<IActionResult> ExportAuditCsv(
            [FromQuery] string? user,
            [FromQuery] string? action,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct)
        {
            if (!_audit.Enabled)
            {
                return Ok(new { Enabled = false });
            }
            var entries = await _audit.ReadRecentAsync(20000, ct);
            var filtered = ApplyAuditFilter(entries, user, action, from, to);

            var sb = new StringBuilder();
            sb.Append('\uFEFF'); // UTF-8 BOM —— 让 Excel 直接识别为 UTF-8
            sb.AppendLine("Timestamp,User,Ip,Action,Target,Summary");
            foreach (var e in filtered)
            {
                sb.Append(CsvEscape(e.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"))).Append(',');
                sb.Append(CsvEscape(e.User)).Append(',');
                sb.Append(CsvEscape(e.Ip)).Append(',');
                sb.Append(CsvEscape(e.Action)).Append(',');
                sb.Append(CsvEscape(e.Target)).Append(',');
                sb.Append(CsvEscape(e.Summary));
                sb.Append('\n');
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"gateway-admin-audit-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // ---------- SSE 实时事件 ----------

        /// <summary>
        /// SSE 长连接：广播写操作事件（路由 / Swagger / 备份 / 配置 等）给所有在线管理面板。
        /// 客户端：<c>new EventSource('/admin-api/events')</c>。
        /// 心跳：每 15s 一次注释行 <c>: ping</c>，避免代理超时关闭连接。
        /// </summary>
        [HttpGet("events")]
        public async Task Events(CancellationToken ct)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no"; // nginx 透传

            var (id, reader) = _bus.Subscribe();
            try
            {
                // 初始注释 + retry 提示
                await Response.WriteAsync(": connected\nretry: 5000\n\n", ct).ConfigureAwait(false);
                await Response.Body.FlushAsync(ct).ConfigureAwait(false);

                while (!ct.IsCancellationRequested)
                {
                    using var hbCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    hbCts.CancelAfter(TimeSpan.FromSeconds(15));
                    bool dataAvailable;
                    try
                    {
                        dataAvailable = await reader.WaitToReadAsync(hbCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        // 心跳：写一个注释行让链路保活
                        await Response.WriteAsync(": ping\n\n", ct).ConfigureAwait(false);
                        await Response.Body.FlushAsync(ct).ConfigureAwait(false);
                        continue;
                    }

                    if (!dataAvailable) break;

                    while (reader.TryRead(out var evt))
                    {
                        var json = JsonSerializer.Serialize(evt, AuditEventSerializerOptions);
                        // SSE 帧：event: <type>\n data: <json>\n\n
                        await Response.WriteAsync($"event: {evt.Type}\ndata: {json}\n\n", ct).ConfigureAwait(false);
                    }
                    await Response.Body.FlushAsync(ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /* 正常断开 */ }
            finally
            {
                _bus.Unsubscribe(id);
            }
        }

        private static readonly JsonSerializerOptions AuditEventSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private static List<GatewayAdminAuditEntry> ApplyAuditFilter(
            IReadOnlyList<GatewayAdminAuditEntry> entries,
            string? user,
            string? action,
            DateTime? from,
            DateTime? to)
        {
            var fromUtc = from?.ToUniversalTime();
            var toUtc = to?.ToUniversalTime();
            return entries.Where(e =>
                (string.IsNullOrEmpty(user) || string.Equals(e.User, user, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(action)
                    || (e.Action != null && e.Action.StartsWith(action, StringComparison.OrdinalIgnoreCase)))
                && (!fromUtc.HasValue || e.Timestamp >= fromUtc.Value)
                && (!toUtc.HasValue || e.Timestamp <= toUtc.Value)
            ).ToList();
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            // 含 , " \r \n 时需要用 " 包围并把内部 " 转义为 ""
            var needQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needQuote) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static bool HasApiEndpointMetadata(ServiceInfo service)
            => service.Metadata != null
               && service.Metadata.TryGetValue(ServiceMetadataKeys.ApiEndpoints, out var json)
               && !string.IsNullOrWhiteSpace(json);

        private static IEnumerable<ServiceApiEndpointInfo> ParseApiEndpoints(ServiceInfo service)
        {
            if (!HasApiEndpointMetadata(service))
            {
                return Enumerable.Empty<ServiceApiEndpointInfo>();
            }
            try
            {
                return JsonSerializer.Deserialize<List<ServiceApiEndpointInfo>>(
                           service.Metadata[ServiceMetadataKeys.ApiEndpoints],
                           ReadOptions)
                       ?? Enumerable.Empty<ServiceApiEndpointInfo>();
            }
            catch
            {
                return Enumerable.Empty<ServiceApiEndpointInfo>();
            }
        }

        private static string? BuildServiceUrl(ServiceInfo service, string path)
        {
            if (string.IsNullOrWhiteSpace(service.ServiceAddress) || service.ServicePort <= 0)
            {
                return null;
            }

            var scheme = service.ServiceProtocol.ToString().ToLowerInvariant();
            var normalizedPath = string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : (path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path);
            return $"{scheme}://{service.ServiceAddress}:{service.ServicePort}{normalizedPath}";
        }

        private static string BuildInstanceKey(ServiceInfo service)
            => $"{service.ServiceProtocol.ToString().ToLowerInvariant()}://{service.ServiceAddress}:{service.ServicePort}";

        // ---------- helpers ----------

        private Task Audit(string action, string? target, string summary, CancellationToken ct)
        {
            var user = HttpContext.User?.Identity?.Name;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            // 先广播事件（in-memory 同步入队），再落审计日志（IO）。
            _bus.Publish(new GatewayAdminEvent
            {
                Type = action,
                User = user,
                Target = target,
                Summary = summary,
            });
            return _audit.AppendAsync(new GatewayAdminAuditEntry
            {
                User = user,
                Ip = ip,
                Action = action,
                Target = target,
                Summary = summary
            }, ct);
        }

        private static string Sha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static bool HasConflict(List<GatewayRoute> routes, GatewayRoute candidate, int ignoreIndex)
        {
            for (var i = 0; i < routes.Count; i++)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }
                var existing = routes[i];
                if (!string.Equals(existing.UpstreamPathTemplate, candidate.UpstreamPathTemplate, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (existing.UpstreamHttpMethod != null && candidate.UpstreamHttpMethod != null
                    && existing.UpstreamHttpMethod.Intersect(candidate.UpstreamHttpMethod, StringComparer.OrdinalIgnoreCase).Any())
                {
                    return true;
                }
                if ((existing.UpstreamHttpMethod is null || existing.UpstreamHttpMethod.Count == 0)
                    && (candidate.UpstreamHttpMethod is null || candidate.UpstreamHttpMethod.Count == 0))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
