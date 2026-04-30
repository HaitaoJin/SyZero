using System.Collections.Generic;
using System.Security.Claims;
using SyZero.Dependency;

namespace SyZero.Runtime.Session
{
    /// <summary>
    /// 运行时会话
    /// </summary>
    public interface ISySession
    {
        ClaimsPrincipal Principal { get; }
        /// <summary>
        /// 获取当前用户ID或空。
        /// 如果没有用户登录，则为空。
        /// </summary>
        long? UserId { get; }

        /// <summary>
        /// 获取当前用户角色或空。
        /// 如果没有用户登录，则为空。
        /// </summary>
        string UserRole { get; }

        /// <summary>
        /// 获取当前用户名称或空。
        /// 如果没有用户登录，则为空。
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// 获取当前用户名称或空。
        /// 如果没有用户登录，则为空。
        /// </summary>
        List<string> Permission { get; }

        /// <summary>
        /// 获取当前用户名称或空。
        /// 如果没有用户登录，则为空。
        /// </summary>
        string Token { get; }

        ISySession Parse(ClaimsPrincipal claimsPrincipal);

        ISySession Parse(string token);
    }
}
