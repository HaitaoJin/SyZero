using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 共用的程序集发现器。
    /// 默认排除 BCL / Microsoft / netstandard 等系统程序集，调用方可追加额外排除规则。
    /// </summary>
    public static class DynamicApiAssemblyScanner
    {
        private static readonly string[] _systemAssemblyPrefixes =
        {
            "System",
            "Microsoft",
            "mscorlib",
            "netstandard"
        };

        /// <summary>
        /// 获取候选程序集集合。
        /// </summary>
        /// <param name="extraExclude">额外的排除谓词；返回 true 表示该程序集应被排除。</param>
        public static IEnumerable<Assembly> GetAssemblies(Func<Assembly, bool>? extraExclude = null)
        {
            // 仅遍历 AppDomain 中“已经加载”的程序集会遗漏尚未被运行时代码触发的
            // Application 层程序集，导致动态应用服务没有进入 MVC ApplicationParts。
            // ReflectionHelper 会根据 DependencyContext 主动加载所有项目程序集；再与
            // AppDomain 合并，兼顾普通宿主、测试宿主以及单文件等特殊运行场景。
            return ReflectionHelper.GetAssemblies()
                .Concat(AppDomain.CurrentDomain.GetAssemblies())
                .Distinct()
                .Where(assembly => !assembly.IsDynamic
                    && !IsSystemAssembly(assembly)
                    && (extraExclude == null || !extraExclude(assembly)))
                .ToArray();
        }

        /// <summary>
        /// 安全获取程序集导出类型，吞掉 <see cref="ReflectionTypeLoadException"/>。
        /// </summary>
        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
        }

        /// <summary>
        /// 判断是否为常见的系统/框架程序集。
        /// </summary>
        public static bool IsSystemAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }

            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (var prefix in _systemAssemblyPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
