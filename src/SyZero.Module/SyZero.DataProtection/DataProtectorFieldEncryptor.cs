using Microsoft.AspNetCore.DataProtection;
using SyZero.Dependency;

namespace SyZero.DataProtection
{
    /// <summary>
    /// 基于 ASP.NET Core <see cref="IDataProtectionProvider"/> 的字段加密器。
    /// </summary>
    public class DataProtectorFieldEncryptor : IFieldEncryptor, ISingletonDependency
    {
        private readonly IDataProtectionProvider _provider;

        /// <summary>
        /// 通过 DI 注入数据保护提供者。
        /// </summary>
        public DataProtectorFieldEncryptor(IDataProtectionProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc/>
        public string? Protect(string? plaintext, string purpose = "syzero-default")
        {
            if (plaintext is null) return null;
            return _provider.CreateProtector(purpose).Protect(plaintext);
        }

        /// <inheritdoc/>
        public string? Unprotect(string? ciphertext, string purpose = "syzero-default")
        {
            if (ciphertext is null) return null;
            return _provider.CreateProtector(purpose).Unprotect(ciphertext);
        }
    }
}
