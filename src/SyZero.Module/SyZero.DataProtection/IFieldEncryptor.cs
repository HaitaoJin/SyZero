namespace SyZero.DataProtection
{
    /// <summary>
    /// 字段级加解密抽象。默认实现 <see cref="DataProtectorFieldEncryptor"/> 包装 ASP.NET Core Data Protection。
    /// </summary>
    public interface IFieldEncryptor
    {
        /// <summary>
        /// 加密明文。
        /// </summary>
        /// <param name="plaintext">明文（null 返回 null）。</param>
        /// <param name="purpose">加密用途；不同 purpose 使用不同密钥。</param>
        string? Protect(string? plaintext, string purpose = "syzero-default");

        /// <summary>
        /// 解密密文。
        /// </summary>
        string? Unprotect(string? ciphertext, string purpose = "syzero-default");
    }
}
