using System;
using SyZero.Serialization;

namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务代理工厂（协议级抽象）
    /// 一个 Provider 可注册多个协议工厂（例如同时支持 HTTP 与 gRPC）
    /// </summary>
    public interface IRemoteServiceProxyFactory
    {
        /// <summary>
        /// 工厂支持的协议
        /// </summary>
        RemoteServiceProtocol Protocol { get; }

        /// <summary>
        /// 创建代理实例
        /// </summary>
        /// <param name="targetType">目标接口类型</param>
        /// <param name="endPoint">解析后的服务端点</param>
        /// <param name="service">服务配置（已合并全局默认值）</param>
        /// <param name="jsonSerialize">JSON 序列化器</param>
        object CreateProxy(Type targetType, string endPoint, RemoteServiceItem service, IJsonSerialize jsonSerialize);
    }
}
