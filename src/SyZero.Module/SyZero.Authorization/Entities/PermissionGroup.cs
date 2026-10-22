using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 权限组实体（用于树形组织权限定义，类似 ABP PermissionGroup）。
    /// </summary>
    public class PermissionGroup : Entity
    {
        /// <summary>
        /// 名称（编程使用，唯一）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 父级权限组 Id（顶层为 null）
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 排序（同级内升序）
        /// </summary>
        public int Sort { get; set; }
    }
}
