using SyZero.Dependency;

namespace SyZero.ObjectMapper
{
    /// <summary>
    /// 实体映射实现此接口
    /// </summary>
    public interface IObjectMapper
    {
        /// <summary>
        /// Obj转Dto
        /// </summary>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source">Obj</param>
        /// <returns></returns>
        TDestination Map<TDestination>(object source);

        /// <summary>
        /// Dto转Obj
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source">Obj</param>
        /// <param name="destination">Dto</param>
        /// <returns></returns>
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
    }
}
