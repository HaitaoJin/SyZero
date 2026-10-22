using System;
using System.Reflection;

namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 共用的类型工具方法。
    /// </summary>
    public static class DynamicApiTypeHelper
    {
        /// <summary>
        /// 判断类型是否为「扩展原始类型」（含可空包装）。
        /// 扩展原始类型 = CLR 原始类型 + string / decimal / DateTime / DateTimeOffset / TimeSpan / Guid。
        /// </summary>
        /// <param name="type">待检测类型。</param>
        /// <param name="includeEnums">是否将枚举也视为原始类型。</param>
        public static bool IsPrimitiveExtendedIncludingNullable(Type type, bool includeEnums = false)
        {
            if (type == null)
            {
                return false;
            }

            if (IsPrimitiveExtended(type, includeEnums))
            {
                return true;
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsPrimitiveExtended(type.GenericTypeArguments[0], includeEnums);
            }

            return false;
        }

        private static bool IsPrimitiveExtended(Type type, bool includeEnums)
        {
            if (type.GetTypeInfo().IsPrimitive)
            {
                return true;
            }

            if (includeEnums && type.GetTypeInfo().IsEnum)
            {
                return true;
            }

            return type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid);
        }
    }
}
