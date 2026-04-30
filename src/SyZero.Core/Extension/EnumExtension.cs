using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SyZero
{
    public static class EnumExtension
    {
        /// <summary>
        /// 返回枚举的Description
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string ToDescription(this Enum e)
        {
            FieldInfo fieldInfo = e.GetType().GetField(e.ToString());
            if (fieldInfo != null)
            {
                DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes != null && attributes.Any())
                {
                    return attributes.FirstOrDefault().Description;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 返回枚举的Description 
        /// </summary>
        /// <param name="enumSubitem"></param>
        /// <returns></returns>
        public static string GetEnumDescription(this Enum enumSubitem)
        {
            string strValue = enumSubitem.ToString();

            FieldInfo fieldinfo = enumSubitem.GetType().GetField(strValue);
            if (fieldinfo == null)
                return null;

            Object[] objs = fieldinfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (objs == null || objs.Length == 0)
            {
                return strValue;
            }
            else
            {
                DescriptionAttribute da = (DescriptionAttribute)objs[0];
                return da.Description;
            }
        }

        public static Dictionary<string, string> ToDictionary<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                       .Cast<T>()
                       .ToDictionary(
                           value => value.ToString(),
                           value => value.ToDescription()
                       );
        }

        public static T GetEnumFromDescription<T>(this string description)
        {
            foreach (var field in typeof(T).GetFields())
            {
                var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes.Length > 0)
                {
                    var desc = ((DescriptionAttribute)attributes[0]).Description;
                    if (desc.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return default(T); // 如果没有找到匹配的描述，则返回 null
        }

    }
}
