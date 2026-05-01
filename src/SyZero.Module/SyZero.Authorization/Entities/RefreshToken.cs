using System;
using System.ComponentModel.DataAnnotations;
using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 刷新令牌持久化记录。Token 字段存储 SHA-256 哈希，不存原始值。
    /// </summary>
    public class RefreshToken : Entity
    {
        public long UserId { get; set; }

        /// <summary>
        /// 原始 token 的 SHA-256 哈希（Base64Url 编码）。
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// 轮转后被替换为哪个新 token（哈希）。用于检测旧 token 复用。
        /// </summary>
        [MaxLength(128)]
        public string? ReplacedByTokenHash { get; set; }

        /// <summary>
        /// 创建时的客户端 IP（可选）。
        /// </summary>
        [MaxLength(64)]
        public string? CreatedByIp { get; set; }

        /// <summary>
        /// 是否仍可用：未撤销且未过期。
        /// </summary>
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
