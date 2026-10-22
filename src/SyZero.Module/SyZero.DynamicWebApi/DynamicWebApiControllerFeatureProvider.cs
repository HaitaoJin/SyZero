using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using SyZero.Application.DynamicApi;
using SyZero.DynamicWebApi.Attributes;

namespace SyZero.DynamicWebApi
{
    /// <summary>
    /// 动态 WebApi 控制器特性提供程序
    /// 用于识别和过滤可作为动态 WebApi 控制器的类型
    /// </summary>
    public class DynamicWebApiControllerFeatureProvider : ControllerFeatureProvider
    {
        // Provider 自身的额外排除特性
        private static readonly Type[] _extraExcludeAttributes = { typeof(NonWebApiServiceAttribute) };

        // 缓存类型判断结果，避免重复反射
        private static readonly ConcurrentDictionary<Type, bool> _controllerCache = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// 判断指定类型是否可以作为动态 WebApi 控制器
        /// </summary>
        protected override bool IsController(TypeInfo typeInfo)
        {
            var type = typeInfo.AsType();
            return _controllerCache.GetOrAdd(type, _ =>
                DynamicApiTypeFilter.IsCandidate(typeInfo, _extraExcludeAttributes));
        }

        /// <summary>
        /// 清除类型缓存（用于测试或动态加载程序集场景）
        /// </summary>
        public static void ClearCache()
        {
            _controllerCache.Clear();
        }
    }
}
