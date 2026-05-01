using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using SyZero.Application.DynamicApi;
using SyZero.DynamicGrpc.Attributes;

namespace SyZero.DynamicGrpc
{
    /// <summary>
    /// 动态 gRPC 服务类型提供程序
    /// 用于识别和过滤可作为动态 gRPC 服务的类型
    /// </summary>
    public class DynamicGrpcServiceTypeProvider
    {
        private readonly DynamicGrpcOptions _options;

        // Provider 自身的额外排除特性
        private static readonly Type[] _extraExcludeAttributes = { typeof(NonGrpcServiceAttribute) };

        // 缓存类型判断结果，避免重复反射
        private static readonly ConcurrentDictionary<Type, bool> _serviceCache = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// 使用指定选项初始化
        /// </summary>
        public DynamicGrpcServiceTypeProvider(DynamicGrpcOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 判断指定类型是否可以作为动态 gRPC 服务
        /// </summary>
        public bool IsGrpcService(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return _serviceCache.GetOrAdd(typeInfo.AsType(), _ =>
                DynamicApiTypeFilter.IsCandidate(typeInfo, _extraExcludeAttributes));
        }

        /// <summary>
        /// 获取服务名称（移除后缀）
        /// </summary>
        public string GetServiceName(string serviceName)
        {
            return DynamicApiNameNormalizer.Normalize(serviceName, _options.RemoveServicePostfixes, _options.GetServiceName);
        }

        /// <summary>
        /// 获取方法名称（移除后缀）
        /// </summary>
        public string GetMethodName(string methodName)
        {
            return DynamicApiNameNormalizer.Normalize(methodName, _options.RemoveMethodPostfixes, _options.GetMethodName);
        }

        /// <summary>
        /// 清除类型缓存（用于测试或动态加载程序集场景）
        /// </summary>
        public static void ClearCache()
        {
            _serviceCache.Clear();
        }
    }
}
