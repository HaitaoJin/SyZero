using System;
using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 角色实体。
    /// </summary>
    public class Role : Entity
    {
        /// <summary>
        /// 角色名称（唯一）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 规范化角色名（大写）
        /// </summary>
        public string NormalizedName { get; set; } = string.Empty;

        /// <summary>
        /// 显示名
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 并发标识
        /// </summary>
        public string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 是否系统内置（不可删除）
        /// </summary>
        public bool IsStatic { get; set; }
    }
}
