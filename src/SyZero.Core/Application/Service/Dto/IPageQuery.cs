namespace SyZero.Application.Service.Dto
{
    /// <summary>
    /// 此接口定义为标准化以请求跳过数量
    /// </summary>
    public interface IPageQuery : ILimitQuery
    {
        /// <summary>
        /// 跳过数量
        /// </summary>
        int Skip { get; set; }
    }
}
