using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SyZero.Dependency;

namespace SyZero
{
    /// <summary>
    /// 数据脱敏器：对包含 <see cref="SensitiveAttribute"/> 标记的字段值进行替换。
    /// </summary>
    public interface IAuditDataMasker
    {
        /// <summary>
        /// 序列化对象为 JSON 字符串，并将带有 <see cref="SensitiveAttribute"/> 的属性脱敏。
        /// </summary>
        string Serialize(object? value);
    }

    /// <summary>
    /// 默认实现：基于反射检查属性的 <see cref="SensitiveAttribute"/>，替换为占位字符串后再 JSON 序列化。
    /// 仅对顶层对象与一层嵌套字典生效，复杂对象树请实现自定义 IAuditDataMasker。
    /// </summary>
    public class DefaultAuditDataMasker : IAuditDataMasker, ISingletonDependency
    {
        /// <inheritdoc/>
        public string Serialize(object? value)
        {
            if (value is null) return "null";

            try
            {
                var masked = MaskValue(value);
                return JsonSerializer.Serialize(masked);
            }
            catch
            {
                return "<unserializable>";
            }
        }

        private static object? MaskValue(object? value)
        {
            if (value is null) return null;

            var type = value.GetType();
            if (type.IsPrimitive || value is string || value is decimal || value is DateTime || value is Guid)
            {
                return value;
            }

            if (value is IDictionary dictionary)
            {
                var dict = new Dictionary<string, object?>();
                foreach (DictionaryEntry de in dictionary)
                {
                    dict[de.Key?.ToString() ?? "null"] = MaskValue(de.Value);
                }
                return dict;
            }

            if (value is IEnumerable enumerable)
            {
                return enumerable.Cast<object?>().Select(MaskValue).ToList();
            }

            // 普通对象：按属性反射，标 [Sensitive] 的替换占位符。
            var result = new Dictionary<string, object?>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                var sensitive = prop.GetCustomAttribute<SensitiveAttribute>();
                object? raw;
                try { raw = prop.GetValue(value); }
                catch { continue; }

                if (sensitive != null)
                {
                    result[prop.Name] = raw is null ? null : sensitive.Placeholder;
                }
                else
                {
                    result[prop.Name] = raw;
                }
            }
            return result;
        }
    }
}
