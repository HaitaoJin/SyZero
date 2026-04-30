using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using SyZero.Application.Attributes;
using SyZero.Application.Service;
using SyZero.Client;
using SyZero.DynamicWebApi.Attributes;

namespace SyZero.DynamicWebApi
{
    /// <summary>
    /// 动态 WebApi 控制器特性提供程序
    /// 用于识别和过滤可作为动态 WebApi 控制器的类型
    /// </summary>
    public class DynamicWebApiControllerFeatureProvider : ControllerFeatureProvider
    {
        // 缓存类型判断结果，避免重复反射
        private static readonly ConcurrentDictionary<Type, bool> _controllerCache = new();

        /// <summary>
        /// 判断指定类型是否可以作为动态 WebApi 控制器
        /// </summary>
        /// <param name="typeInfo">类型信息</param>
        /// <returns>如果类型是有效的动态 WebApi 控制器则返回 true</returns>
        protected override bool IsController(TypeInfo typeInfo)
        {
            var type = typeInfo.AsType();

            return _controllerCache.GetOrAdd(type, t => IsValidDynamicController(typeInfo));
        }

        /// <summary>
        /// 验证类型是否为有效的动态控制器
        /// </summary>
        private static bool IsValidDynamicController(TypeInfo typeInfo)
        {
            // 基本条件检查：必须实现 IDynamicApi，必须是公开的非抽象非泛型类
            if (!IsDynamicApiType(typeInfo))
            {
                return false;
            }

            // 不能是 IFallback 类型（熔断降级类）
            if (typeof(IFallback).IsAssignableFrom(typeInfo.AsType()))
            {
                return false;
            }

            // 必须有 DynamicApiAttribute 标记
            var DynamicApiAttr = ReflectionHelper.GetSingleAttributeOrDefaultByFullSearch<DynamicApiAttribute>(typeInfo);
            if (DynamicApiAttr == null)
            {
                return false;
            }

            // 不能有 NonDynamicApiAttribute 排除标记
            if (ReflectionHelper.GetSingleAttributeOrDefaultByFullSearch<NonDynamicApiAttribute>(typeInfo) != null)
            {
                return false;
            }

            // 不能有 NonWebApiServiceAttribute 排除标记（用于排除某个 DynamicApi 不注册为 WebApi）
            if (ReflectionHelper.GetSingleAttributeOrDefaultByFullSearch<NonWebApiServiceAttribute>(typeInfo) != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查类型是否符合动态 WebApi 的基本要求
        /// </summary>
        private static bool IsDynamicApiType(TypeInfo typeInfo)
        {
            return typeof(IDynamicApi).IsAssignableFrom(typeInfo.AsType()) &&
                   typeInfo.IsPublic &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.IsGenericType;
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