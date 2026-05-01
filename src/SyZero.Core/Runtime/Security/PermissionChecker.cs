using SyZero.Runtime.Session;
using SyZero.Util;

namespace SyZero.Runtime.Security
{
    /// <summary>
    /// 权限检查（静态门面）。
    /// <para>
    /// 注：推荐通过 <see cref="IPermissionChecker"/> 注入式使用，便于单元测试与替换实现。
    /// 此静态入口保留以兼容旧代码，内部委托到 DI 容器中的 <see cref="IPermissionChecker"/>，
    /// 若未注册则回退到基于 <see cref="ISySession"/> 的简易检查。
    /// </para>
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
            var checker = TryResolve<IPermissionChecker>();
            if (checker != null)
            {
                return checker.IsHas(permission);
            }

            ISySession? sySession = TryResolve<ISySession>();
            if (sySession?.Permission != null && sySession.Permission.Contains(permission))
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

        private static T? TryResolve<T>() where T : class
        {
            try
            {
                return SyZeroUtil.GetService<T>();
            }
            catch
            {
                return null;
            }
        }
    }
}
