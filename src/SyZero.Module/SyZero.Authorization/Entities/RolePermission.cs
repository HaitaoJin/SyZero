using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 角色权限授权关联实体。
    /// </summary>
    public class RolePermission : Entity
    {
        /// <summary>
        /// 角色 Id
        /// </summary>
        public long RoleId { get; set; }

        /// <summary>
        /// 权限名称（与 <see cref="Permission.Name"/> 对应）
        /// </summary>
        public string PermissionName { get; set; } = string.Empty;

        /// <summary>
        /// 是否授予；false 表示显式拒绝（高于角色继承）
        /// </summary>
        public bool IsGranted { get; set; } = true;
    }
}
