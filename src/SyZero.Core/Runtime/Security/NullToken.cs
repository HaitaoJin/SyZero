using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace SyZero.Runtime.Security;

/// <summary>
/// 未安装认证模块时使用的空令牌实现，使不需要认证能力的宿主仍可启动。
/// </summary>
internal sealed class NullToken : IToken
{
    public ClaimsPrincipal? GetPrincipal(string token) => null;

    public string CreateAccessToken(IEnumerable<Claim> claims)
        => throw new InvalidOperationException("尚未注册令牌实现，请添加 SyZero.Web.Common 或 SyZero.Authorization 模块。");
}
