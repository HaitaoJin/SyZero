namespace SyZero.Application.Service.Dto
{
    /// <summary>
    /// ISkipQuery实现
    /// </summary>
    public class PageQueryDto : LimitQueryDto, IPageQuery
    {
        public int Skip { get; set; } = 0;
    }
}
