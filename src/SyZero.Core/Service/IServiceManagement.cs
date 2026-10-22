using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SyZero.Service
{
    public interface IServiceManagement
    {
        #region 服务查询

        /// <summary>
        /// 根据服务名称获取服务实例
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <returns>服务实例列表</returns>
        Task<List<ServiceInfo>> GetService(string serviceName);

        /// <summary>
        /// 获取健康的服务实例
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <returns>健康的服务实例列表</returns>
        Task<List<ServiceInfo>> GetHealthyServices(string serviceName);

        /// <summary>
        /// 获取单个服务实例（支持负载均衡策略）
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <returns>服务实例</returns>
        Task<ServiceInfo> GetServiceInstance(string serviceName);

        /// <summary>
        /// 获取所有已注册的服务名称
        /// </summary>
        /// <returns>服务名称列表</returns>
        Task<List<string>> GetAllServices();

        #endregion

        #region 服务注册/注销

        /// <summary>
        /// 注册服务实例
        /// </summary>
        /// <param name="serviceInfo">服务信息</param>
        /// <returns></returns>
        Task RegisterService(ServiceInfo serviceInfo);

        /// <summary>
        /// 注销服务实例
        /// </summary>
        /// <param name="serviceId">服务ID</param>
        /// <returns></returns>
        Task DeregisterService(string serviceId);

        #endregion

        #region 健康检查

        /// <summary>
        /// 检查服务是否健康
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <returns>是否健康</returns>
        Task<bool> IsServiceHealthy(string serviceName);

        #endregion

        #region 服务订阅

        /// <summary>
        /// 订阅服务变更通知
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <param name="callback">变更回调</param>
        /// <returns></returns>
        Task Subscribe(string serviceName, Action<List<ServiceInfo>> callback);

        /// <summary>
        /// 取消订阅服务变更通知
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <returns></returns>
        Task Unsubscribe(string serviceName);

        #endregion
    }
}
