using System;

namespace SyZero.Audit
{
    /// <summary>
    /// 标记字段/属性为敏感数据，写入审计日志时会被脱敏。
    /// 默认脱敏方式由 <see cref="IAuditDataMasker"/> 实现决定。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class SensitiveAttribute : Attribute
    {
        /// <summary>
        /// 脱敏后的占位字符串，默认 "***"。
        /// </summary>
        public string Placeholder { get; set; } = "***";
    }
}
