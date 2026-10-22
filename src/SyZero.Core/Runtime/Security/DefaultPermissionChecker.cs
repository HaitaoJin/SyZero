using SyZero.Dependency;
using SyZero.Runtime.Session;

namespace SyZero.Runtime.Security
{
    /// <summary>
    /// 默认权限检查器：基于当前会话 <see cref="ISySession.Permission"/> 列表判断。
    /// 通过 <see cref="IScopedDependency"/> 自动注册到 DI 容器（与 <see cref="ISySession"/> 同生命周期）。
    /// </summary>
    public class DefaultPermissionChecker : IPermissionChecker, IScopedDependency
    {
        private readonly ISySession _sySession;

        public DefaultPermissionChecker(ISySession sySession)
        {
            _sySession = sySession;
        }

        public bool IsHas(string permission)
        {
            if (_sySession?.Permission != null && _sySession.Permission.Contains(permission))
            {
                return true;
            }
            return false;
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
