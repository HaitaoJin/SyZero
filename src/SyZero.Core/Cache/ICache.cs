using Dynamitey.DynamicObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Cache
{
    public interface ICache
    {
        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool Exist(string key);
        /// <summary>
        /// 根据前缀获取所有的key
        /// 例如：pro_*
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string[] GetKeys(string pattern);
        /// <summary>
        /// 获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="isExisted">是否成功</param>
        /// <returns></returns>
        T Get<T>(string key);
        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key);
        /// <summary>
        /// 移除 (异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(string key);
        /// <summary>
        /// 重置值
        /// </summary>
        /// <param name="key"></param>
        void Refresh(string key);
        /// <summary>
        /// 重置值 (异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RefreshAsync(string key);
        /// <summary>
        /// 存储值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">值</param>
        /// <param name="exprireTime">过期时间（秒） 默认24小时</param>
        void Set<T>(string key, T value, int exprireTime = 24  * 60 * 60);
        /// <summary>
        /// 存储值 (异步)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="exprireTime">过期时间（秒） 默认24小时</param>
        /// <returns></returns>
        Task SetAsync<T>(string key, T value, int exprireTime = 24  * 60 * 60);
    }
}
