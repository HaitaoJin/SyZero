using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 权限实体。
    /// </summary>
    public class Permission : Entity
    {
        /// <summary>
        /// 权限名称（编程使用，全局唯一，建议形如 "MyApp.Users.Create"）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 所属权限组 Id（可空：未分组的散权限）
        /// </summary>
        public long? GroupId { get; set; }

        /// <summary>
        /// 父权限 Id（用于权限的层级关系，可空）
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }
    }
}
