using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SyZero.Application.Attributes;
using SyZero.Application.Service;
using SyZero.Client;

namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 共用的类型筛选规则。
    /// 集中实现「实现 <see cref="IDynamicApi"/> + 公有非抽象非泛型 + 必须有 <see cref="DynamicApiAttribute"/>
    /// + 没有 <see cref="NonDynamicApiAttribute"/> + 不是 <see cref="IFallback"/>」这一组通用判定，
    /// 各 Provider 仅需追加自身的排除特性。
    /// </summary>
    public static class DynamicApiTypeFilter
    {
        /// <summary>
        /// 是否满足动态 API 的基本结构要求（不要求 <see cref="DynamicApiAttribute"/>）。
        /// </summary>
        public static bool IsStructuralCandidate(TypeInfo typeInfo, bool requireClass = true)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if (!typeof(IDynamicApi).IsAssignableFrom(typeInfo.AsType()))
            {
                return false;
            }

            if (!typeInfo.IsPublic)
            {
                return false;
            }

            if (typeInfo.IsGenericType)
            {
                return false;
            }

            if (requireClass)
            {
                if (!typeInfo.IsClass || typeInfo.IsAbstract)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 完整判定一个类型是否是某个 Provider 想要发布的动态 API 类型。
        /// </summary>
        /// <param name="typeInfo">候选类型。</param>
        /// <param name="extraExcludeAttributes">Provider 自身的排除特性类型集合（如 NonWebApiServiceAttribute / NonGrpcServiceAttribute）。</param>
        /// <param name="requireClass">是否要求是非抽象类（gRPC / WebApi 实际上都要求 true）。</param>
        public static bool IsCandidate(TypeInfo typeInfo, IEnumerable<Type>? extraExcludeAttributes = null, bool requireClass = true)
        {
            if (!IsStructuralCandidate(typeInfo, requireClass))
            {
                return false;
            }

            // 熔断降级类不发布
            if (typeof(IFallback).IsAssignableFrom(typeInfo.AsType()))
            {
                return false;
            }

            if (DynamicApiAttributeCache.GetTypeAttributeOrNull<DynamicApiAttribute>(typeInfo) == null)
            {
                return false;
            }

            if (DynamicApiAttributeCache.GetTypeAttributeOrNull<NonDynamicApiAttribute>(typeInfo) != null)
            {
                return false;
            }

            if (extraExcludeAttributes != null)
            {
                foreach (var attrType in extraExcludeAttributes)
                {
                    if (attrType == null)
                    {
                        continue;
                    }

                    if (HasAttributeByFullSearch(typeInfo, attrType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 判断方法是否被 Provider 标记为「不发布」。
        /// 同时检查通用的 <see cref="NonDynamicApiAttribute"/> / <see cref="NonDynamicMethodAttribute"/>
        /// 与 Provider 自身的排除特性集合。
        /// </summary>
        public static bool IsMethodExcluded(MemberInfo memberInfo, IEnumerable<Type>? extraExcludeAttributes = null)
        {
            if (memberInfo == null)
            {
                return false;
            }

            if (DynamicApiAttributeCache.GetMemberAttributeOrNull<NonDynamicApiAttribute>(memberInfo) != null)
            {
                return true;
            }

            if (DynamicApiAttributeCache.GetMemberAttributeOrNull<NonDynamicMethodAttribute>(memberInfo) != null)
            {
                return true;
            }

            if (extraExcludeAttributes == null)
            {
                return false;
            }

            return extraExcludeAttributes.Any(attrType =>
                attrType != null && memberInfo.GetCustomAttributes(attrType, true).Any());
        }

        private static bool HasAttributeByFullSearch(TypeInfo typeInfo, Type attributeType)
        {
            if (typeInfo.GetCustomAttributes(attributeType, true).Any())
            {
                return true;
            }

            foreach (var implInterface in typeInfo.ImplementedInterfaces)
            {
                if (HasAttributeByFullSearch(implInterface.GetTypeInfo(), attributeType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
