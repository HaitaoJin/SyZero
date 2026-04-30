using System;
using SyZero.Serialization;

namespace SyZero.Feign.Proxy
{
    /// <summary>
    /// Feign 代理工厂接口
    /// </summary>
    public interface IFeignProxyFactory
    {
        /// <summary>
        /// 获取支持的协议类型
        /// </summary>
        FeignProtocol Protocol { get; }

        /// <summary>
        /// 创建代理实例
        /// </summary>
        /// <param name="targetType">目标接口类型</param>
        /// <param name="endPoint">服务端点地址</param>
        /// <param name="feignService">服务配置</param>
        /// <param name="jsonSerialize">JSON 序列化器</param>
        /// <returns>代理实例</returns>
        object CreateProxy(Type targetType, string endPoint, FeignService feignService, IJsonSerialize jsonSerialize);
    }
}
