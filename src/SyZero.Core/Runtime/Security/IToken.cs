using System.Collections.Generic;
using System.Security.Claims;

namespace SyZero.Runtime.Security
{
    public interface IToken
    {
        /// <summary>
        /// 此方法用解码字符串token，并返回秘钥的信息对象
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        ClaimsPrincipal GetPrincipal(string token);

        /// <summary>
        /// 获取Token
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        string CreateAccessToken(IEnumerable<Claim> claims);
    }
}
