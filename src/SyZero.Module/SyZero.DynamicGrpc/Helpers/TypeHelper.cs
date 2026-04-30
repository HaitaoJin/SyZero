using System;
using System.Reflection;

namespace SyZero.DynamicGrpc.Helpers
{
    /// <summary>
    /// 类型辅助类
    /// </summary>
    internal static class TypeHelper
    {
        /// <summary>
        /// 判断类型是否为原始类型（包括可空类型）
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="includeEnums">是否包括枚举</param>
        /// <returns>是否为原始类型</returns>
        public static bool IsPrimitiveExtendedIncludingNullable(Type type, bool includeEnums = false)
        {
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

        /// <summary>
        /// 判断类型是否为扩展的原始类型
        /// </summary>
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

            return type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }

        /// <summary>
        /// 判断类型是否可以作为 gRPC 服务类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>是否可以作为 gRPC 服务</returns>
        public static bool IsValidGrpcServiceType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPublic &&
                   typeInfo.IsClass &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.IsGenericType;
        }

        /// <summary>
        /// 判断类型是否为有效的 gRPC 服务接口
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>是否为有效的服务接口</returns>
        public static bool IsValidGrpcServiceInterface(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPublic && typeInfo.IsInterface;
        }
    }
}
