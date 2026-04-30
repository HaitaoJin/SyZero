using Microsoft.Extensions.Logging;
using SyZero.Domain.Repository;
using SyZero.ObjectMapper;
using SyZero.Util;

namespace SyZero
{
    /// <summary>
    /// 服务基类
    /// </summary>
    public abstract class SyZeroServiceBase
    {
        /// <summary>
        /// 持久化
        /// </summary>
        public IUnitOfWork UnitOfWork => SyZeroUtil.GetScopeService<IUnitOfWork>();

        /// <summary>
        /// 日志
        /// </summary>
        public ILogger<SyZeroServiceBase> Logger => SyZeroUtil.GetService<ILogger<SyZeroServiceBase>>();

        /// <summary>
        /// 实体映射
        /// </summary>
        public IObjectMapper ObjectMapper => SyZeroUtil.GetService<IObjectMapper>();
    }
}
