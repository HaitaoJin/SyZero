using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;
using SyZero.Util;

namespace SyZero.AspNetCore.Middleware
{
    /// <summary>
    /// 权限中间件
    /// </summary>
    public class SyAuthMiddleware : IMiddleware
    {
        private static readonly ClaimsPrincipal Anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private readonly IToken _token;

        public SyAuthMiddleware(IToken token)
        {
            _token = token;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using (SyZeroUtil.BeginScope(context.RequestServices))
            {
                // 仅使用 HttpContext.User 存储当前用户，
                // Thread.CurrentPrincipal 在 ASP.NET Core 中已不再推荐（异步上下文切换不可靠）。
                context.User = Anonymous;

                if (context.Request.Headers.TryGetValue("Authorization", out var token)
                    && token.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var tokenString = token.ToString().Substring("Bearer ".Length).Trim();

                    if (!string.IsNullOrWhiteSpace(tokenString))
                    {
                        // 如果令牌有效，则将用户信息添加到上下文中
                        var claimsPrincipal = _token.GetPrincipal(tokenString);

                        if (claimsPrincipal != null)
                        {
                            context.User = claimsPrincipal;
                            SyZeroUtil.GetScopeService<ISySession>()?.Parse(claimsPrincipal);
                        }
                    }
                }

                await next.Invoke(context);
            }
        }
    }
}
