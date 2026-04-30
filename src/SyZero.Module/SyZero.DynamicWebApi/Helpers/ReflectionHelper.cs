using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SyZero.DynamicWebApi.Helpers
{
    /// <summary>
    /// 反射辅助类
    /// </summary>
    internal static class ReflectionHelper
    {
        // 缓存特性查找结果
        private static readonly ConcurrentDictionary<(Type, Type), object> _attributeCache = new();
        private static readonly ConcurrentDictionary<(MemberInfo, Type), object> _memberAttributeCache = new();

        /// <summary>
        /// 通过完整搜索获取单个特性（包括接口）
        /// </summary>
        /// <typeparam name="TAttribute">特性类型</typeparam>
        /// <param name="info">类型信息</param>
        /// <returns>特性实例或 null</returns>
        public static TAttribute GetSingleAttributeOrDefaultByFullSearch<TAttribute>(TypeInfo info)
            where TAttribute : Attribute
        {
            var key = (info.AsType(), typeof(TAttribute));
            
            return (TAttribute)_attributeCache.GetOrAdd(key, _ => 
                GetSingleAttributeOrDefaultByFullSearchInternal<TAttribute>(info));
        }

        /// <summary>
        /// 实际执行搜索的内部方法
        /// </summary>
        private static TAttribute GetSingleAttributeOrDefaultByFullSearchInternal<TAttribute>(TypeInfo info)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);
            
            // 首先在当前类型上查找
            if (info.IsDefined(attributeType, true))
            {
                return info.GetCustomAttributes(attributeType, true).Cast<TAttribute>().First();
            }

            // 在实现的接口上查找
            foreach (var implInterface in info.ImplementedInterfaces)
            {
                var result = GetSingleAttributeOrDefaultByFullSearchInternal<TAttribute>(implInterface.GetTypeInfo());
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取成员的单个特性
        /// </summary>
        /// <typeparam name="TAttribute">特性类型</typeparam>
        /// <param name="memberInfo">成员信息</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="inherit">是否包含继承的特性</param>
        /// <returns>特性实例或默认值</returns>
        public static TAttribute GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default, bool inherit = true)
            where TAttribute : Attribute
        {
            if (memberInfo == null)
            {
                return defaultValue;
            }

            var key = (memberInfo, typeof(TAttribute));
            
            var result = _memberAttributeCache.GetOrAdd(key, _ =>
            {
                var attributeType = typeof(TAttribute);
                if (memberInfo.IsDefined(attributeType, inherit))
                {
                    return memberInfo.GetCustomAttributes(attributeType, inherit).Cast<TAttribute>().FirstOrDefault();
                }
                return null;
            });

            return (TAttribute)result ?? defaultValue;
        }

        /// <summary>
        /// 获取成员的单个特性，不存在则返回 null
        /// </summary>
        /// <typeparam name="TAttribute">特性类型</typeparam>
        /// <param name="memberInfo">成员信息</param>
        /// <param name="inherit">是否包含继承的特性</param>
        /// <returns>特性实例或 null</returns>
        public static TAttribute GetSingleAttributeOrNull<TAttribute>(this MemberInfo memberInfo, bool inherit = true)
            where TAttribute : Attribute
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException(nameof(memberInfo));
            }

            var attrs = memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).ToArray();
            return attrs.Length > 0 ? (TAttribute)attrs[0] : default;
        }

        /// <summary>
        /// 获取类型或其基类型上的单个特性
        /// </summary>
        /// <typeparam name="TAttribute">特性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="inherit">是否包含继承的特性</param>
        /// <returns>特性实例或 null</returns>
        public static TAttribute GetSingleAttributeOfTypeOrBaseTypesOrNull<TAttribute>(this Type type, bool inherit = true)
            where TAttribute : Attribute
        {
            var attr = type.GetTypeInfo().GetSingleAttributeOrNull<TAttribute>(inherit);
            if (attr != null)
            {
                return attr;
            }

            if (type.GetTypeInfo().BaseType == null)
            {
                return null;
            }

            return type.GetTypeInfo().BaseType.GetSingleAttributeOfTypeOrBaseTypesOrNull<TAttribute>(inherit);
        }

        /// <summary>
        /// 获取所有动态 WebApi 相关的程序集
        /// </summary>
        /// <returns>程序集列表</returns>
        public static IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && !IsSystemAssembly(assembly));
        }

        /// <summary>
        /// 判断是否为系统程序集
        /// </summary>
        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return name != null && (
                name.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            _attributeCache.Clear();
            _memberAttributeCache.Clear();
        }
    }
}