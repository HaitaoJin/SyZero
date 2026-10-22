using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 共用的特性查找缓存。
    /// 在 <see cref="ReflectionHelper"/> 现有反射方法之上叠加缓存，
    /// 供 DynamicWebApi / DynamicGrpc 等 Provider 高频调用。
    ///
    /// <para>
    /// 缓存键基数由当前进程已加载程序集中的 <see cref="TypeInfo"/> / <see cref="MemberInfo"/> 数量决定，
    /// 在常规场景下有界（不会随请求量增长），因此使用无界 <see cref="ConcurrentDictionary{TKey, TValue}"/> 即可。
    /// 若运行时存在动态 <c>AssemblyBuilder</c>/<c>Collectible AssemblyLoadContext</c> 持续加载新类型的场景，
    /// 请在卸载窗口主动调用 <see cref="Clear"/> 释放缓存，避免长期持有 <see cref="MemberInfo"/> 引用。
    /// </para>
    /// </summary>
    public static class DynamicApiAttributeCache
    {
        private static readonly ConcurrentDictionary<(Type, Type), object> _typeAttributeCache = new ConcurrentDictionary<(Type, Type), object>();
        private static readonly ConcurrentDictionary<(MemberInfo, Type), object> _memberAttributeCache = new ConcurrentDictionary<(MemberInfo, Type), object>();

        /// <summary>
        /// 在类型及其实现的接口上完整搜索单个特性（带缓存）。
        /// </summary>
        public static TAttribute? GetTypeAttributeOrNull<TAttribute>(TypeInfo info)
            where TAttribute : Attribute
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var key = (info.AsType(), typeof(TAttribute));
            return (TAttribute?)_typeAttributeCache.GetOrAdd(key, _ => info.GetSingleAttributeOrDefaultByFullSearch<TAttribute>()!);
        }

        /// <summary>
        /// 获取成员上的单个特性（带缓存）。
        /// </summary>
        public static TAttribute? GetMemberAttributeOrNull<TAttribute>(MemberInfo memberInfo, bool inherit = true)
            where TAttribute : Attribute
        {
            if (memberInfo == null)
            {
                return null;
            }

            var key = (memberInfo, typeof(TAttribute));
            return (TAttribute?)_memberAttributeCache.GetOrAdd(key, _ => ReflectionHelper.GetSingleAttributeOrDefault<TAttribute>(memberInfo, null, inherit)!);
        }

        /// <summary>
        /// 清空全部特性缓存（用于动态加载程序集 / 单元测试场景）。
        /// </summary>
        public static void Clear()
        {
            _typeAttributeCache.Clear();
            _memberAttributeCache.Clear();
        }
    }
}
