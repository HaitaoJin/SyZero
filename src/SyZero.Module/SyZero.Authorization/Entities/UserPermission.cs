using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 直接授予用户的权限关联（覆盖角色权限）。
    /// </summary>
    public class UserPermission : Entity
    {
        /// <summary>
        /// 用户 Id
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string PermissionName { get; set; } = string.Empty;

        /// <summary>
        /// 是否授予；false 表示显式拒绝（高于角色继承）
        /// </summary>
        public bool IsGranted { get; set; } = true;
    }
}
