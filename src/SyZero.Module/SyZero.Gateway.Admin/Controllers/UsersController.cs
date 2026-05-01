#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyZero.Gateway.Admin.Auditing;
using SyZero.Gateway.Admin.Auth;
using SyZero.Gateway.Admin.Events;

namespace SyZero.Gateway.Admin.Controllers
{
    /// <summary>
    /// 管理台用户 CRUD（仅 file 用户可写；appsettings 内置账号只读）。
    /// 所有写操作要求 Admin 角色（中间件已强制），<c>POST /admin-api/me/password</c> 任何登录用户可调用。
    /// </summary>
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("admin-api")]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserResolver _resolver;
        private readonly IGatewayAdminUserStore _store;
        private readonly IGatewayAdminAuditLog _audit;
        private readonly IGatewayAdminEventBus _bus;

        public UsersController(
            IUserResolver resolver,
            IGatewayAdminUserStore store,
            IGatewayAdminAuditLog audit,
            IGatewayAdminEventBus bus)
        {
            _resolver = resolver;
            _store = store;
            _audit = audit;
            _bus = bus;
        }

        public sealed class UserDto
        {
            public string Name { get; set; } = string.Empty;
            public string Role { get; set; } = GatewayAdminRoles.Admin;
            public bool IsBuiltIn { get; set; }
        }

        public sealed class CreateUserRequest
        {
            public string? Name { get; set; }
            public string? Password { get; set; }
            public string? Role { get; set; }
        }

        public sealed class UpdateUserRequest
        {
            public string? Password { get; set; }
            public string? Role { get; set; }
        }

        public sealed class ChangePasswordRequest
        {
            public string? OldPassword { get; set; }
            public string? NewPassword { get; set; }
        }

        // ---- 列表（任何登录用户可读，密码不返回） ----
        [HttpGet("users")]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var all = await _resolver.ListAsync(ct);
            return Ok(all.Select(u => new UserDto
            {
                Name = u.Name,
                Role = u.Role,
                IsBuiltIn = u.IsBuiltIn,
            }));
        }

        [HttpPost("users")]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest(new { title = "用户名与密码不能为空" });
            }
            if (req.Role is not null && !GatewayAdminRoles.IsValid(req.Role))
            {
                return BadRequest(new { title = "Role 只能是 Admin 或 Reader" });
            }
            var existed = await _resolver.FindAsync(req.Name!, ct);
            if (existed is not null)
            {
                return Conflict(new { title = $"用户 \"{req.Name}\" 已存在（{(existed.IsBuiltIn ? "appsettings 内置" : "users.json")}）" });
            }
            var ok = await _store.AddAsync(new GatewayAdminUser
            {
                Name = req.Name!,
                Password = req.Password!,
                Role = GatewayAdminRoles.Normalize(req.Role),
            }, ct);
            if (!ok) return Conflict(new { title = "用户已存在" });
            await Audit("user.create", req.Name, $"创建用户 {req.Name}（角色：{GatewayAdminRoles.Normalize(req.Role)}）", ct);
            return Ok(new { name = req.Name });
        }

        [HttpPut("users/{name}")]
        public async Task<IActionResult> Update(string name, [FromBody] UpdateUserRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name) || req is null)
            {
                return BadRequest(new { title = "请求参数不完整" });
            }
            if (req.Role is not null && !GatewayAdminRoles.IsValid(req.Role))
            {
                return BadRequest(new { title = "Role 只能是 Admin 或 Reader" });
            }
            var current = await _resolver.FindAsync(name, ct);
            if (current is null) return NotFound();
            if (current.IsBuiltIn)
            {
                return BadRequest(new { title = "appsettings 内置账号不可在面板修改，请直接编辑 appsettings.json" });
            }
            // 防止把最后一个 Admin 降级
            if (req.Role is not null
                && string.Equals(current.Role, GatewayAdminRoles.Admin, StringComparison.OrdinalIgnoreCase)
                && string.Equals(GatewayAdminRoles.Normalize(req.Role), GatewayAdminRoles.Reader, StringComparison.OrdinalIgnoreCase))
            {
                var admins = await _resolver.CountAdminsAsync(ct);
                if (admins <= 1)
                {
                    return Conflict(new { title = "不能降级最后一个 Admin 账号" });
                }
            }
            var ok = await _store.UpdateAsync(name, req.Password, req.Role, ct);
            if (!ok) return NotFound();
            var changes = req.Password is not null
                ? (req.Role is not null ? "改密码 + 角色 → " + GatewayAdminRoles.Normalize(req.Role) : "改密码")
                : (req.Role is not null ? "角色 → " + GatewayAdminRoles.Normalize(req.Role) : "无变更");
            await Audit("user.update", name, $"更新用户 {name}：{changes}", ct);
            return NoContent();
        }

        [HttpDelete("users/{name}")]
        public async Task<IActionResult> Delete(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest();
            var current = await _resolver.FindAsync(name, ct);
            if (current is null) return NotFound();
            if (current.IsBuiltIn)
            {
                return BadRequest(new { title = "appsettings 内置账号不可删除" });
            }
            var me = HttpContext.User?.Identity?.Name;
            if (string.Equals(me, name, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { title = "不能删除当前登录账号" });
            }
            if (string.Equals(current.Role, GatewayAdminRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var admins = await _resolver.CountAdminsAsync(ct);
                if (admins <= 1)
                {
                    return Conflict(new { title = "不能删除最后一个 Admin 账号" });
                }
            }
            var ok = await _store.RemoveAsync(name, ct);
            if (!ok) return NotFound();
            await Audit("user.delete", name, $"删除用户 {name}", ct);
            return NoContent();
        }

        // 任意登录用户改自己的密码（Reader 也允许；中间件 /admin-api/me/* 已放行）。
        [HttpPost("me/password")]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            if (req is null || string.IsNullOrEmpty(req.OldPassword) || string.IsNullOrEmpty(req.NewPassword))
            {
                return BadRequest(new { title = "原密码与新密码不能为空" });
            }
            var me = HttpContext.User?.Identity?.Name;
            if (string.IsNullOrEmpty(me)) return Unauthorized();
            var u = await _resolver.FindAsync(me!, ct);
            if (u is null) return Unauthorized();
            if (u.IsBuiltIn)
            {
                return BadRequest(new { title = "appsettings 内置账号请在配置文件中修改密码" });
            }
            var validated = await _resolver.ValidateAsync(me!, req.OldPassword!, ct);
            if (validated is null)
            {
                return Unauthorized(new { title = "原密码不正确" });
            }
            await _store.UpdateAsync(me!, req.NewPassword, null, ct);
            await Audit("me.password", me, $"用户 {me} 修改了自己的密码", ct);
            return NoContent();
        }

        private Task Audit(string action, string? target, string summary, CancellationToken ct)
        {
            var user = HttpContext.User?.Identity?.Name;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
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
                Summary = summary,
            }, ct);
        }
    }
}
