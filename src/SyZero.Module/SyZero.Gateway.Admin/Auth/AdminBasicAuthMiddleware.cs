#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 仅作用于 /admin、/admin-api 前缀的鉴权中间件。
    /// 凭据通过 <see cref="IUserResolver"/> 解析（合并 appsettings.Users[] 与 users.json）。
    /// 支持两种认证方式：
    /// 1. Cookie：登录页提交后由 <c>POST /admin-api/auth/login</c> 端点写入 <see cref="AuthCookieName"/>；
    /// 2. HTTP Basic：兼容 curl / 自动化脚本。
    /// 通过校验后注入 <see cref="ClaimsPrincipal"/>，包含 <see cref="ClaimTypes.Name"/>、<see cref="ClaimTypes.Role"/> 与自定义 <c>gw:builtin</c>。
    /// 对 <c>/admin-api/*</c> 的非 GET 请求强制要求 Admin 角色，Reader 返回 403。
    /// </summary>
    public sealed class AdminBasicAuthMiddleware
    {
        public const string AuthCookieName = "syz_admin_auth";

        // 旧版兼容键名（已废弃，仅在错误提示中引用，不再用于鉴权）。
        public const string UsernameKey = "Gateway:Admin:Username";
        public const string PasswordKey = "Gateway:Admin:Password";

        public const string BuiltInClaimType = "gw:builtin";

        // 不需要鉴权即可访问的子路径（自身就是登录入口或登录端点）。
        private static readonly string[] AnonymousPaths =
        {
            "/admin/login",
            "/admin-api/auth/login",
            "/admin-api/auth/captcha",
        };

        private readonly RequestDelegate _next;
        private readonly GatewayAdminCookieProtector? _cookieProtector;

        public AdminBasicAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>由依赖注入使用的构造函数，启用受 Data Protection 保护的 Cookie。</summary>
        public AdminBasicAuthMiddleware(RequestDelegate next, GatewayAdminCookieProtector cookieProtector)
        {
            _next = next;
            _cookieProtector = cookieProtector;
        }

        public async Task InvokeAsync(HttpContext context, IUserResolver resolver)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            foreach (var anon in AnonymousPaths)
            {
                if (PathEquals(path, anon))
                {
                    await _next(context);
                    return;
                }
            }

            // 1) 优先尝试 cookie。
            GatewayAdminUser? user = null;
            if (context.Request.Cookies.TryGetValue(AuthCookieName, out var cookieToken)
                && TryParseCookieToken(cookieToken, out var cu, out var cp))
            {
                user = await resolver.ValidateAsync(cu, cp, context.RequestAborted);
            }

            // 2) 回退到 Authorization: Basic ...
            if (user is null)
            {
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrEmpty(authHeader)
                    && TryParseBasic(authHeader, out var bu, out var bp))
                {
                    user = await resolver.ValidateAsync(bu, bp, context.RequestAborted);
                }
            }

            if (user is null)
            {
                await ChallengeAsync(context, path);
                return;
            }

            // 注入 ClaimsPrincipal。
            var role = GatewayAdminRoles.Normalize(user.Role);
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Role, role),
                new(BuiltInClaimType, user.IsBuiltIn ? "true" : "false"),
            };
            var identity = new ClaimsIdentity(claims, authenticationType: "GatewayAdmin");
            context.User = new ClaimsPrincipal(identity);

            // Reader 角色：禁止对 /admin-api/* 的写操作（POST/PUT/DELETE/PATCH）。
            // 例外：/admin-api/auth/logout 与 /admin-api/me/* 允许 Reader 自助调用（修改自己密码 / 登出）。
            if (string.Equals(role, GatewayAdminRoles.Reader, StringComparison.OrdinalIgnoreCase)
                && path.StartsWith("/admin-api", StringComparison.OrdinalIgnoreCase)
                && !IsSafeMethod(context.Request.Method)
                && !IsReaderAllowedWritePath(path))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"error\":\"forbidden\",\"reason\":\"reader-readonly\"}");
                return;
            }

            await _next(context);
        }

        private static bool IsSafeMethod(string method)
            => string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase);

        private static bool IsReaderAllowedWritePath(string path)
            => PathEquals(path, "/admin-api/auth/logout")
            || PathEquals(path, "/admin-api/me/password");

        private bool TryParseCookieToken(string token, out string user, out string password)
        {
            if (_cookieProtector is not null
                && _cookieProtector.TryUnprotect(token, out user, out password))
            {
                return true;
            }

            // 兼容升级前已经签发的 Base64 Cookie；新 Cookie 一律使用 Data Protection。
            return TryParseToken(token, out user, out password);
        }

        private static bool PathEquals(string path, string expected)
            => string.Equals(path.TrimEnd('/'), expected, StringComparison.OrdinalIgnoreCase);

        internal static bool TryParseBasic(string headerValue, out string user, out string password)
        {
            user = string.Empty;
            password = string.Empty;

            if (!AuthenticationHeaderValue.TryParse(headerValue, out var parsed)
                || !string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(parsed.Parameter))
            {
                return false;
            }

            return TryParseToken(parsed.Parameter, out user, out password);
        }

        internal static bool TryParseToken(string base64Token, out string user, out string password)
        {
            user = string.Empty;
            password = string.Empty;
            try
            {
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(base64Token));
                var idx = raw.IndexOf(':');
                if (idx < 0)
                {
                    return false;
                }
                user = raw[..idx];
                password = raw[(idx + 1)..];
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        internal static bool FixedTimeEquals(string a, string b)
        {
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length)
            {
                CryptographicOperations.FixedTimeEquals(ba, ba);
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(ba, bb);
        }

        private static async Task ChallengeAsync(HttpContext context, string path)
        {
            // 对 API 调用：直接 401 JSON，不带 WWW-Authenticate（避免浏览器原生 popup）。
            if (path.StartsWith("/admin-api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"title\":\"未登录或会话已过期\",\"loginUrl\":\"/admin/login\"}");
                return;
            }

            // 对页面访问：跳转登录页，并保留 returnUrl。
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect("/admin/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
        }
    }
}
