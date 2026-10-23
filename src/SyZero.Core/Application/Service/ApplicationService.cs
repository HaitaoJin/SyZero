using SyZero.Dependency;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;

namespace SyZero.Application.Service
{
    public class ApplicationService : SyZeroServiceBase, IApplicationService
    {
        public ApplicationService(SyZeroServiceDependencies dependencies, ISySession sySession) : base(dependencies)
            => SySession = sySession;

        public ISySession SySession { get; }

        protected virtual void CheckPermission(string? permissionName = null)
        {
            if (!SySession.UserId.HasValue)
            {
                throw new SyMessageException(SyMessageBoxStatus.UnAuthorized);
            }
            if (!string.IsNullOrEmpty(permissionName))
            {
                if (!PermissionChecker.IsHas(permissionName))
                {
                    throw new SyMessageException(SyMessageBoxStatus.NoPermission);
                }
            }
        }
    }

}
