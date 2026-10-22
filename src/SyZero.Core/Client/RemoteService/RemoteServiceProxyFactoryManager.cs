using System;
using System.Collections.Generic;
using System.Linq;
using SyZero.Serialization;

namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务代理工厂管理器
    /// 维护 协议 → 工厂 的映射，由 Provider 内部使用
    /// </summary>
    public class RemoteServiceProxyFactoryManager
    {
        private readonly Dictionary<RemoteServiceProtocol, IRemoteServiceProxyFactory> _factories
            = new Dictionary<RemoteServiceProtocol, IRemoteServiceProxyFactory>();

        /// <summary>
        /// 注册（或覆盖）某个协议的工厂
        /// </summary>
        public void RegisterFactory(IRemoteServiceProxyFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factories[factory.Protocol] = factory;
        }

        /// <summary>
        /// 获取指定协议的工厂
        /// </summary>
        public IRemoteServiceProxyFactory GetFactory(RemoteServiceProtocol protocol)
        {
            if (_factories.TryGetValue(protocol, out var factory))
            {
                return factory;
            }

            throw new NotSupportedException($"不支持的协议类型: {protocol}");
        }

        /// <summary>
        /// 创建代理（按服务配置中的协议自动派发到对应工厂）
        /// </summary>
        public object CreateProxy(Type targetType, string endPoint, RemoteServiceItem service, IJsonSerialize jsonSerialize)
        {
            var factory = GetFactory(service.Protocol);
            return factory.CreateProxy(targetType, endPoint, service, jsonSerialize);
        }

        /// <summary>
        /// 已注册的协议列表
        /// </summary>
        public IEnumerable<RemoteServiceProtocol> GetRegisteredProtocols()
        {
            return _factories.Keys.ToList();
        }

        /// <summary>
        /// 协议是否已注册
        /// </summary>
        public bool IsProtocolRegistered(RemoteServiceProtocol protocol)
        {
            return _factories.ContainsKey(protocol);
        }
    }
}
