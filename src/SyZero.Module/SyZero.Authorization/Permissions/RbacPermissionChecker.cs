using System.Collections.Generic;
using System.Linq;
using SyZero.Authorization.Entities;
using SyZero.Dependency;
using SyZero.Domain.Repository;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;

namespace SyZero.Authorization.Permissions
{
    /// <summary>
    /// 基于 RBAC 数据库的权限检查器（替换 SyZero 默认 PermissionChecker）。
    /// 优先级：UserPermission（拒绝/授予）覆盖 RolePermission；其次按 ISySession.Permission 兜底。
    /// </summary>
    public class RbacPermissionChecker : IPermissionChecker, IScopedDependency
    {
        private readonly ISySession _session;
        private readonly IRepository<UserRole> _userRoles;
        private readonly IRepository<RolePermission> _rolePermissions;
        private readonly IRepository<UserPermission> _userPermissions;

        public RbacPermissionChecker(
            ISySession session,
            IRepository<UserRole> userRoles,
            IRepository<RolePermission> rolePermissions,
            IRepository<UserPermission> userPermissions)
        {
            _session = session;
            _userRoles = userRoles;
            _rolePermissions = rolePermissions;
            _userPermissions = userPermissions;
        }

        public bool IsHas(string permission)
        {
            if (string.IsNullOrEmpty(permission))
            {
                return true;
            }

            if (_session?.UserId is not long userId || userId <= 0)
            {
                return false;
            }

            var direct = _userPermissions
                .GetList(p => p.UserId == userId && p.PermissionName == permission)
                .FirstOrDefault();
            if (direct != null)
            {
                return direct.IsGranted;
            }

            var roleIds = _userRoles.GetList(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToList();
            if (roleIds.Count == 0)
            {
                return false;
            }

            var grants = _rolePermissions
                .GetList(rp => rp.PermissionName == permission && roleIds.Contains(rp.RoleId))
                .ToList();
            if (grants.Count == 0)
            {
                return false;
            }

            // 任一显式拒绝即拒绝；否则任一授予即通过
            if (grants.Any(g => !g.IsGranted)) return false;
            return grants.Any(g => g.IsGranted);
        }

        public void Authorize(string permission)
        {
            if (!IsHas(permission))
            {
                throw new SyMessageException(SyMessageBoxStatus.NoPermission);
            }
        }
    }
}
