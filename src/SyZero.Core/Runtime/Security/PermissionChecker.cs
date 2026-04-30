using SyZero.Runtime.Session;
using SyZero.Util;

namespace SyZero.Runtime.Security
{
    /// <summary>
    /// 权限检查
    /// </summary>
    public static class PermissionChecker
    {
        /// <summary>
        /// 是否拥有权限
        /// </summary>
        /// <param name="permission">权限值</param>
        /// <returns></returns>
        public static bool IsHas(string permission)
        {
            ISySession sySession = SyZeroUtil.GetService<ISySession>();
            if (sySession.Permission != null && sySession.Permission.Contains(permission))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查权限 Exception 无权限
        /// </summary>
        /// <param name="permission"></param>
        public static void Authorize(string permission)
        {
            if (!IsHas(permission))
            {
                throw new SyMessageException(SyMessageBoxStatus.NoPermission);
            }
        }




    }
}
