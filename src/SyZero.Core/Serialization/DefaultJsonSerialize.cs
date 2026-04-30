using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SyZero.Dependency;

namespace SyZero.Serialization
{
    /// <summary>
    /// 基于 System.Text.Json 的默认 JSON 序列化实现
    /// </summary>
    public class DefaultJsonSerialize : IJsonSerialize, ISingletonDependency
    {
        private readonly JsonSerializerOptions _options;

        public DefaultJsonSerialize()
        {
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Json反序列化
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="jsonText">Json字符串</param>
        /// <returns></returns>
        public T JSONToObject<T>(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(jsonText, _options);
            }
            catch (Exception ex)
            {
                throw new Exception("DefaultJsonSerialize.JSONToObject(): " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Obj序列化
        /// </summary>
        /// <param name="obj">Obj</param>
        /// <returns></returns>
        public string ObjectToJSON(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Serialize(obj, _options);
            }
            catch (Exception ex)
            {
                throw new Exception("DefaultJsonSerialize.ObjectToJSON(): " + ex.Message, ex);
            }
        }
    }
}
