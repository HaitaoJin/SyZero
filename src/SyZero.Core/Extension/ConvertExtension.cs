using System;
using System.ComponentModel;
using System.Reflection;

namespace SyZero
{
    public static class ConvertExtension
    {
        /// <summary>
        /// string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToStr<T>(this T value)
        {
            if (value == null) return string.Empty;

            return value.ToString();
        }

        /// <summary>
        /// int
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ToInt32<T>(this T value)
        {
            if (value == null) return 0;

            if (value.GetType().BaseType == typeof(Enum))
                return Convert.ToInt32(value);

            if (!Int32.TryParse(value.ToStr(), out int result))
                return 0;

            return result;
        }

        /// <summary>
        /// float
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float ToFloat<T>(this T value)
        {
            if (value == null) return 0;

            float result = 0;

            if (!float.TryParse(value.ToStr(), out result))
                return 0;

            return result;
        }

        /// <summary>
        /// double
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ToDouble<T>(this T value)
        {
            if (value == null) return 0;

            double result = 0;

            if (!double.TryParse(value.ToStr(), out result))
                return 0;

            return result;
        }

        /// <summary>
        /// long
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long ToLong<T>(this T value)
        {
            if (value == null) return 0;

            long result = 0;

            if (!long.TryParse(value.ToStr(), out result))
                return 0;

            return result;
        }

        /// <summary>
        /// decimal
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static decimal ToDecimal<T>(this T value)
        {
            if (value == null) return 0;

            decimal result = 0;

            if (!decimal.TryParse(value.ToStr(), out result))
                return 0;

            return result;
        }

        /// <summary>
        /// Guid
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid ToGuid<T>(this T value)
        {
            if (value == null) return Guid.Empty;

            Guid result = Guid.Empty;

            if (!Guid.TryParse(value.ToStr(), out result))
                return Guid.Empty;

            return result;
        }

        /// <summary>
        /// Guid?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid? ToGuidNull<T>(this T value)
        {
            if (value == null) return null;

            Guid result = Guid.Empty;

            if (!Guid.TryParse(value.ToStr(), out result))
                return null;

            return result;
        }

        /// <summary>
        /// GuidString
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToGuidStr<T>(this T value)
        {
            return value.ToGuid().ToStr();
        }

        /// <summary>
        /// DateTime
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime ToDateTime<T>(this T value)
        {
            if (value == null) return DateTime.MinValue;

            DateTime result = DateTime.MinValue;

            if (!DateTime.TryParse(value.ToStr(), out result))
                return DateTime.MinValue;

            return result;
        }

        /// <summary>
        /// DateTime?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime? ToDateTimeNull<T>(this T value)
        {
            if (value == null) return null;

            DateTime result = DateTime.MinValue;

            if (!DateTime.TryParse(value.ToStr(), out result))
                return null;

            return result;
        }

        /// <summary>
        /// 格式的 时间 字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="FormatStr"></param>
        /// <returns></returns>
        public static string ToDateTimeFormat<T>(this T value, string FormatStr = "yyyy-MM-dd")
        {
            var datetime = value.ToDateTime();
            if (datetime.ToShortDateString() == DateTime.MinValue.ToShortDateString())
                return String.Empty;
            else
                return datetime.ToString(FormatStr);
        }

        /// <summary>
        /// bool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ToBool<T>(this T value)
        {
            if (value == null) return false;

            bool result = false;

            if (!bool.TryParse(value.ToStr(), out result))
                return false;

            return result;
        }

        /// <summary>
        /// byte[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToBytes<T>(this T value)
        {
            if (value == null) return null;

            return value as byte[];
        }

        /// <summary>
        /// byte[]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string value)
        {
            if (value == null) return null;

            return System.Text.Encoding.Default.GetBytes(value);
        }

        /// <summary>
        /// 时间戳转为C#格式时间
        /// </summary>
        /// <param name=”timeStamp”></param>
        /// <returns></returns>
        public static DateTime ToTime<T>(this int timeStamp)
        {
            var dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime); return dtStart.Add(toNow);
        }

        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name=”time”></param>
        /// <returns></returns>
        public static int ToTimeInt<T>(this DateTime time)
        {
            var startTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
            return (int)((time) - startTime).TotalSeconds;
        }


        /// <summary>
        /// 将一个对象转换为指定类型
        /// </summary>
        /// <param name="obj">待转换的对象</param>
        /// <param name="type">目标类型</param>
        /// <returns>转换后的对象</returns>
        public static object ConvertToObject(this object obj, Type type)
        {
            if (type == null) return obj;
            if (obj == null) return type.IsValueType ? Activator.CreateInstance(type) : null;

            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (type.IsAssignableFrom(obj.GetType())) // 如果待转换对象的类型与目标类型兼容，则无需转换
            {
                return obj;
            }
            else if ((underlyingType ?? type).IsEnum) // 如果待转换的对象的基类型为枚举
            {
                if (underlyingType != null && string.IsNullOrEmpty(obj.ToString())) // 如果目标类型为可空枚举，并且待转换对象为null 则直接返回null值
                {
                    return null;
                }
                else
                {
                    return Enum.Parse(underlyingType ?? type, obj.ToString());
                }
            }
            else if (typeof(IConvertible).IsAssignableFrom(underlyingType ?? type)) // 如果目标类型的基类型实现了IConvertible，则直接转换
            {
                try
                {
                    return Convert.ChangeType(obj, underlyingType ?? type, null);
                }
                catch
                {
                    return underlyingType == null ? Activator.CreateInstance(type) : null;
                }
            }
            else
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(obj.GetType()))
                {
                    return converter.ConvertFrom(obj);
                }
                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    object o = constructor.Invoke(null);
                    PropertyInfo[] propertys = type.GetProperties();
                    Type oldType = obj.GetType();
                    foreach (PropertyInfo property in propertys)
                    {
                        PropertyInfo p = oldType.GetProperty(property.Name);
                        if (property.CanWrite && p != null && p.CanRead)
                        {
                            property.SetValue(o, ConvertToObject(p.GetValue(obj, null), property.PropertyType), null);
                        }
                    }
                    return o;
                }
            }
            return obj;
        }

    }
}
