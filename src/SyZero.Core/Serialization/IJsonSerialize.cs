namespace SyZero.Serialization
{
    /// <summary>
    /// Json序列化
    /// </summary>
    public interface IJsonSerialize
    {
        /// <summary>
        /// Obj序列化
        /// </summary>
        /// <param name="obj">Obj</param>
        /// <returns></returns>
        string ObjectToJSON(object obj);

        /// <summary>
        /// Json反序列化
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="jsonText">Json字符串</param>
        /// <returns></returns>
        T JSONToObject<T>(string jsonText);
    }
}
