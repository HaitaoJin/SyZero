using SyZero.Domain.Entities;

namespace SyZero.Authorization.Entities
{
    /// <summary>
    /// 用户与角色的关联实体。
    /// </summary>
    public class UserRole : Entity
    {
        /// <summary>
        /// 用户 Id
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// 角色 Id
        /// </summary>
        public long RoleId { get; set; }
    }
}
