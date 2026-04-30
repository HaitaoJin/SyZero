using System;
using System.Collections.Generic;
using System.Linq;
using SyZero.Serialization;

namespace SyZero.Feign.Proxy
{
    /// <summary>
    /// Feign 代理工厂管理器
    /// </summary>
    public class FeignProxyFactoryManager
    {
        private readonly Dictionary<FeignProtocol, IFeignProxyFactory> _factories;

        /// <summary>
        /// 初始化代理工厂管理器
        /// </summary>
        public FeignProxyFactoryManager()
        {
            _factories = new Dictionary<FeignProtocol, IFeignProxyFactory>();

            // 注册内置的代理工厂
            RegisterFactory(new HttpProxyFactory());
            RegisterFactory(new GrpcProxyFactory());
        }

        /// <summary>
        /// 注册代理工厂
        /// </summary>
        /// <param name="factory">代理工厂实例</param>
        public void RegisterFactory(IFeignProxyFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[factory.Protocol] = factory;
        }

        /// <summary>
        /// 获取代理工厂
        /// </summary>
        /// <param name="protocol">协议类型</param>
        /// <returns>代理工厂</returns>
        public IFeignProxyFactory GetFactory(FeignProtocol protocol)
        {
            if (_factories.TryGetValue(protocol, out var factory))
            {
                return factory;
            }

            throw new NotSupportedException($"不支持的协议类型: {protocol}");
        }

        /// <summary>
        /// 创建代理
        /// </summary>
        /// <param name="targetType">目标接口类型</param>
        /// <param name="endPoint">服务端点</param>
        /// <param name="feignService">服务配置</param>
        /// <param name="jsonSerialize">JSON 序列化器</param>
        /// <returns>代理实例</returns>
        public object CreateProxy(Type targetType, string endPoint, FeignService feignService, IJsonSerialize jsonSerialize)
        {
            var factory = GetFactory(feignService.Protocol);
            return factory.CreateProxy(targetType, endPoint, feignService, jsonSerialize);
        }

        /// <summary>
        /// 获取所有已注册的协议类型
        /// </summary>
        public IEnumerable<FeignProtocol> GetRegisteredProtocols()
        {
            return _factories.Keys.ToList();
        }

        /// <summary>
        /// 检查协议是否已注册
        /// </summary>
        /// <param name="protocol">协议类型</param>
        /// <returns>是否已注册</returns>
        public bool IsProtocolRegistered(FeignProtocol protocol)
        {
            return _factories.ContainsKey(protocol);
        }
    }
}
