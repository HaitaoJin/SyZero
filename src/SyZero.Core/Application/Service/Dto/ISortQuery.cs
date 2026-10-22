namespace SyZero.Application.Service.Dto
{
    public interface ISortQuery
    {
        /// <summary>
        /// 排序
        /// </summary>
        /// <example>
        /// Name Aes,Age Desc
        /// </example>
        string Sort { get; set; }
    }
}
