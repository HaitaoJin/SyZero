using System;
using System.Reflection;
using SyZero.Dependency;

namespace SyZero.ObjectMapper
{
    /// <summary>
    /// 基于反射的默认对象映射实现
    /// 将源对象的属性值映射到目标对象的同名属性
    /// </summary>
    public class DefaultObjectMapper : IObjectMapper, ISingletonDependency
    {
        /// <summary>
        /// 将源对象映射到目标类型的新实例
        /// </summary>
        /// <typeparam name="TDestination">目标类型</typeparam>
        /// <param name="source">源对象</param>
        /// <returns>映射后的目标对象</returns>
        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
            {
                return default;
            }

            var destination = Activator.CreateInstance<TDestination>();
            MapProperties(source, destination);
            return destination;
        }

        /// <summary>
        /// 将源对象的属性映射到现有的目标对象
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TDestination">目标类型</typeparam>
        /// <param name="source">源对象</param>
        /// <param name="destination">目标对象</param>
        /// <returns>映射后的目标对象</returns>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null)
            {
                return destination;
            }

            if (destination == null)
            {
                destination = Activator.CreateInstance<TDestination>();
            }

            MapProperties(source, destination);
            return destination;
        }

        /// <summary>
        /// 将源对象的属性值复制到目标对象的同名属性
        /// </summary>
        /// <param name="source">源对象</param>
        /// <param name="destination">目标对象</param>
        private void MapProperties(object source, object destination)
        {
            var sourceType = source.GetType();
            var destinationType = destination.GetType();

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var sourceProperty in sourceProperties)
            {
                if (!sourceProperty.CanRead)
                {
                    continue;
                }

                foreach (var destinationProperty in destinationProperties)
                {
                    if (!destinationProperty.CanWrite)
                    {
                        continue;
                    }

                    // 属性名称匹配（忽略大小写）
                    if (!string.Equals(sourceProperty.Name, destinationProperty.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var sourceValue = sourceProperty.GetValue(source);
                    
                    // 类型兼容性检查
                    if (sourceValue == null)
                    {
                        if (!destinationProperty.PropertyType.IsValueType || 
                            Nullable.GetUnderlyingType(destinationProperty.PropertyType) != null)
                        {
                            destinationProperty.SetValue(destination, null);
                        }
                    }
                    else if (destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        destinationProperty.SetValue(destination, sourceValue);
                    }
                    else if (IsConvertible(sourceProperty.PropertyType, destinationProperty.PropertyType))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(sourceValue, destinationProperty.PropertyType);
                            destinationProperty.SetValue(destination, convertedValue);
                        }
                        catch
                        {
                            // 转换失败，跳过此属性
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 检查源类型是否可以转换为目标类型
        /// </summary>
        private bool IsConvertible(Type sourceType, Type destinationType)
        {
            // 处理可空类型
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var underlyingDestType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            // 基本类型之间的转换
            if (underlyingSourceType.IsPrimitive && underlyingDestType.IsPrimitive)
            {
                return true;
            }

            // 字符串与其他类型的转换
            if (underlyingSourceType == typeof(string) || underlyingDestType == typeof(string))
            {
                return true;
            }

            // DateTime 转换
            if (underlyingSourceType == typeof(DateTime) || underlyingDestType == typeof(DateTime))
            {
                return true;
            }

            // Decimal 转换
            if (underlyingSourceType == typeof(decimal) || underlyingDestType == typeof(decimal))
            {
                return true;
            }

            return false;
        }
    }
}
