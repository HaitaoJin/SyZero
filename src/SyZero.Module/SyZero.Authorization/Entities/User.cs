using System;
using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 用户实体。
    /// </summary>
    public class User : Entity
    {
        /// <summary>
        /// 登录名（唯一）
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 规范化登录名（大写，用于唯一性查询）
        /// </summary>
        public string NormalizedUserName { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 规范化邮箱
        /// </summary>
        public string? NormalizedEmail { get; set; }

        /// <summary>
        /// 邮箱是否已确认
        /// </summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// 密码哈希
        /// </summary>
        public string? PasswordHash { get; set; }

        /// <summary>
        /// 安全戳（修改密码/重置时变更）
        /// </summary>
        public string? SecurityStamp { get; set; }

        /// <summary>
        /// 并发标识
        /// </summary>
        public string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 手机号是否已确认
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// 是否启用双因素认证
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// 锁定到期时间
        /// </summary>
        public DateTimeOffset? LockoutEnd { get; set; }

        /// <summary>
        /// 是否允许被锁定
        /// </summary>
        public bool LockoutEnabled { get; set; } = true;

        /// <summary>
        /// 失败登录尝试次数
        /// </summary>
        public int AccessFailedCount { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 显示名
        /// </summary>
        public string? DisplayName { get; set; }
    }
}
