using System.Threading.Tasks;

namespace SyZero.EventBus
{
    /// <summary>
    /// 动态集成事件处理器接口
    /// </summary>
    public interface IDynamicEventHandler
    {
        /// <summary>
        /// 处理动态事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        /// <returns></returns>
        Task HandleAsync(string eventName, dynamic eventData);
    }
}
