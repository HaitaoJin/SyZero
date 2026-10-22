namespace SyZero.Application.Service.Dto
{
    /// <summary>
    /// 此接口定义为标准化以请求有限的结果
    /// </summary>
    public interface ILimitQuery
    {
        /// <summary>
        /// 返回数量
        /// </summary>
        int Limit { get; set; }
    }
}
