namespace SyZero.Application.Service.Dto
{
    public interface IPageResult<T> : IListResult<T>
    {
        /// <summary>
        /// 总数
        /// </summary>
        int total { get; set; }
    }
}
