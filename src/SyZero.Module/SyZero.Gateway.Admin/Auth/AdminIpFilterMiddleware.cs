#nullable enable
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 管理面板 IP 黑白名单中间件：在 Auth 之前执行，命中黑名单或不在白名单内直接返回 403。
    /// 配置参见 <see cref="GatewayAdminIpFilterOptions"/>。
    /// </summary>
    public sealed class AdminIpFilterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptionsMonitor<GatewayAdminIpFilterOptions> _options;
        // 本地缓存：避免每次请求都重建 matcher。OptionsMonitor.OnChange 时重建。
        private IpAclMatcher _allowMatcher;
        private IpAclMatcher _denyMatcher;
        private GatewayAdminIpFilterOptions _snapshot;
        private readonly object _lock = new();

        public AdminIpFilterMiddleware(RequestDelegate next, IOptionsMonitor<GatewayAdminIpFilterOptions> options)
        {
            _next = next;
            _options = options;
            _snapshot = options.CurrentValue;
            _allowMatcher = new IpAclMatcher(_snapshot.Allow);
            _denyMatcher = new IpAclMatcher(_snapshot.Deny);
            options.OnChange(o =>
            {
                lock (_lock)
                {
                    _snapshot = o;
                    _allowMatcher = new IpAclMatcher(o.Allow);
                    _denyMatcher = new IpAclMatcher(o.Deny);
                }
            });
        }

        public async Task InvokeAsync(HttpContext context)
        {
            GatewayAdminIpFilterOptions snap;
            IpAclMatcher allow, deny;
            lock (_lock) { snap = _snapshot; allow = _allowMatcher; deny = _denyMatcher; }

            // 两个名单都为空 —— 完全跳过判定。
            if (allow.IsEmpty && deny.IsEmpty)
            {
                await _next(context);
                return;
            }

            var ip = ResolveClientIp(context, snap);
            if (deny.Matches(ip))
            {
                await Forbid(context, "客户端 IP 已被拒绝");
                return;
            }
            if (!allow.IsEmpty && !allow.Matches(ip))
            {
                await Forbid(context, "客户端 IP 不在允许列表中");
                return;
            }
            await _next(context);
        }

        private static IPAddress? ResolveClientIp(HttpContext context, GatewayAdminIpFilterOptions opts)
        {
            if (opts.TrustForwardedFor && !string.IsNullOrEmpty(opts.ForwardedHeaderName))
            {
                var header = context.Request.Headers[opts.ForwardedHeaderName].ToString();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    var first = header.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (first.Length > 0 && IPAddress.TryParse(first[0], out var fwdIp))
                    {
                        return fwdIp;
                    }
                }
            }
            return context.Connection.RemoteIpAddress;
        }

        private static async Task Forbid(HttpContext context, string reason)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/admin-api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync($"{{\"title\":\"{reason}\"}}");
            }
            else
            {
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(reason);
            }
        }
    }
}
