using Microsoft.Extensions.Logging;
using SyZero.Domain.Repository;
using SyZero.ObjectMapper;
using SyZero.Dependency;

namespace SyZero
{
    /// <summary>
    /// 服务基类
    /// </summary>
    public abstract class SyZeroServiceBase
    {
        private readonly SyZeroServiceDependencies _dependencies;

        /// <summary>新代码应通过构造函数显式注入服务依赖。</summary>
        protected SyZeroServiceBase(SyZeroServiceDependencies dependencies)
            => _dependencies = dependencies ?? throw new System.ArgumentNullException(nameof(dependencies));
        /// <summary>
        /// 持久化
        /// </summary>
        public IUnitOfWork UnitOfWork => _dependencies.UnitOfWork;

        /// <summary>
        /// 日志
        /// </summary>
        public ILogger<SyZeroServiceBase> Logger => _dependencies.Logger;

        /// <summary>
        /// 实体映射
        /// </summary>
        public IObjectMapper ObjectMapper => _dependencies.ObjectMapper;

        /// <summary>权限检查。</summary>
        protected Runtime.Security.IPermissionChecker PermissionChecker => _dependencies.PermissionChecker;
    }
}
