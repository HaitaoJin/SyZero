using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SyZero.Gateway.Admin.Auth;

namespace SyZero.Gateway.Admin.Controllers
{
    /// <summary>
    /// 网关管理登录 / 登出端点（写入 / 清除 cookie）。
    /// 对外路径：<c>POST /admin-api/auth/login</c>、<c>POST /admin-api/auth/logout</c>。
    /// </summary>
    [ApiController]
    [Route("admin-api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserResolver _userResolver;
        private readonly IFailedLoginTracker _failedLogins;
        private readonly IOptionsMonitor<GatewayAdminRateLimitOptions> _rateLimitOptions;
        private readonly ICaptchaStore _captchaStore;

        public AuthController(
            IUserResolver userResolver,
            IFailedLoginTracker failedLogins,
            IOptionsMonitor<GatewayAdminRateLimitOptions> rateLimitOptions,
            ICaptchaStore captchaStore)
        {
            _userResolver = userResolver;
            _failedLogins = failedLogins;
            _rateLimitOptions = rateLimitOptions;
            _captchaStore = captchaStore;
        }

        /// <summary>
        /// 解析用于限流计数的客户端标识。开启 <c>TrustForwardedFor</c> 时取转发头第一个 IP，否则用 RemoteIpAddress。
        /// </summary>
        internal string ResolveClientKey()
        {
            var opts = _rateLimitOptions.CurrentValue;
            if (opts.TrustForwardedFor && !string.IsNullOrWhiteSpace(opts.ForwardedHeaderName))
            {
                if (HttpContext.Request.Headers.TryGetValue(opts.ForwardedHeaderName, out var values))
                {
                    foreach (var raw in values)
                    {
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        // X-Forwarded-For 可能是 "client, proxy1, proxy2" —— 取第一个
                        var first = raw.Split(',')[0].Trim();
                        if (first.Length > 0) return first;
                    }
                }
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public sealed class LoginRequest
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool Remember { get; set; }
            /// <summary>验证码 ID（仅在超阈值后与服务器同步使用）。</summary>
            public string? CaptchaId { get; set; }
            /// <summary>验证码答案。</summary>
            public string? CaptchaAnswer { get; set; }
        }

        /// <summary>
        /// 颁发一份验证码。返回 <c>{ id, question }</c>，客户端在下一次登录请求中携带
        /// <c>CaptchaId</c> + <c>CaptchaAnswer</c>。
        /// </summary>
        [HttpGet("captcha")]
        public IActionResult Captcha()
        {
            var c = _captchaStore.Issue();
            return Ok(new { id = c.Id, question = c.Question });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var clientKey = ResolveClientKey();
            var lockoutSeconds = _failedLogins.GetLockoutSeconds(clientKey);
            if (lockoutSeconds > 0)
            {
                Response.Headers["Retry-After"] = lockoutSeconds.ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    title = $"登录失败次数过多，请在 {lockoutSeconds} 秒后再试",
                    retryAfter = lockoutSeconds,
                });
            }

            if (request is null
                || string.IsNullOrEmpty(request.Username)
                || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { title = "用户名或密码不能为空" });
            }

            // 验证码门控：在当前失败计数达到阈值后要求携带验证码。
            var captchaThreshold = _rateLimitOptions.CurrentValue.EffectiveCaptchaThreshold;
            if (captchaThreshold > 0
                && _failedLogins.GetFailureCount(clientKey) >= captchaThreshold)
            {
                if (string.IsNullOrEmpty(request.CaptchaId)
                    || string.IsNullOrEmpty(request.CaptchaAnswer)
                    || !_captchaStore.TryConsume(request.CaptchaId, request.CaptchaAnswer))
                {
                    return BadRequest(new
                    {
                        title = string.IsNullOrEmpty(request.CaptchaId) ? "请输入验证码" : "验证码错误或已过期",
                        requireCaptcha = true,
                    });
                }
            }

            var matched = await _userResolver.ValidateAsync(request.Username, request.Password, HttpContext.RequestAborted);
            if (matched is null)
            {
                var failures = _failedLogins.RecordFailure(clientKey);
                var remaining = Math.Max(0, _failedLogins.MaxFailures - failures);
                var requireCaptcha = captchaThreshold > 0 && failures >= captchaThreshold;
                return Unauthorized(new
                {
                    title = remaining > 0
                        ? $"用户名或密码错误（剩余尝试 {remaining} 次）"
                        : "登录失败次数过多，账号已临时锁定",
                    failures,
                    remaining,
                    requireCaptcha,
                });
            }

            _failedLogins.Reset(clientKey);
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.Username}:{request.Password}"));
            var options = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = "/",
                Expires = request.Remember ? DateTimeOffset.UtcNow.AddDays(7) : null,
            };
            Response.Cookies.Append(AdminBasicAuthMiddleware.AuthCookieName, token, options);

            return Ok(new { username = matched.Name, role = matched.Role, isBuiltIn = matched.IsBuiltIn });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AdminBasicAuthMiddleware.AuthCookieName, new CookieOptions
            {
                Path = "/",
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
            });
            return Ok();
        }
    }
}
