#nullable enable
using System;
using Microsoft.AspNetCore.DataProtection;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 使用 ASP.NET Core Data Protection 保护管理端 Cookie 中的凭据。
    /// Cookie 仍携带用户名与密码，以便密码或角色修改后现有会话立即失效，
    /// 但内容不再以可逆的 Base64 明文暴露。
    /// </summary>
    public sealed class GatewayAdminCookieProtector
    {
        private const string Purpose = "SyZero.Gateway.Admin.Cookie.v1";
        private readonly IDataProtector _protector;

        public GatewayAdminCookieProtector(IDataProtectionProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            _protector = provider.CreateProtector(Purpose);
        }

        /// <summary>创建受保护的 Cookie 令牌。</summary>
        public string Protect(string username, string password)
        {
            ArgumentNullException.ThrowIfNull(username);
            ArgumentNullException.ThrowIfNull(password);
            return _protector.Protect(username + ":" + password);
        }

        /// <summary>解析并验证受保护的 Cookie 令牌。</summary>
        public bool TryUnprotect(string token, out string username, out string password)
        {
            username = string.Empty;
            password = string.Empty;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                var raw = _protector.Unprotect(token);
                var separator = raw.IndexOf(':');
                if (separator < 0)
                {
                    return false;
                }

                username = raw[..separator];
                password = raw[(separator + 1)..];
                return username.Length > 0;
            }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
            {
                return false;
            }
        }
    }
}
