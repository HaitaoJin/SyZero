using System;

namespace SyZero.DataProtection
{
    /// <summary>
    /// 标记一个字段/属性需要进行可逆加密。
    /// 通常配合 <see cref="IFieldEncryptor"/> 在写入存储或日志前加密、读取后解密。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class EncryptedAttribute : Attribute
    {
        /// <summary>
        /// 加密用途（Purpose）。不同 Purpose 的密钥相互隔离。
        /// </summary>
        public string Purpose { get; }

        /// <summary>
        /// 创建 Encrypted 标记。
        /// </summary>
        /// <param name="purpose">加密用途；同一字段在不同业务上下文使用不同 Purpose 可达到密钥隔离。</param>
        public EncryptedAttribute(string purpose = "syzero-default")
        {
            Purpose = purpose;
        }
    }
}
