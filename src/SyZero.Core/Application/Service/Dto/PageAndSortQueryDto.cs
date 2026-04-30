namespace SyZero.Application.Service.Dto
{
    public class PageAndSortQueryDto : PageQueryDto, IPageAndSortQuery, IPageQuery, ISortQuery, ILimitQuery
    {
        public string Sort { get; set; }
    }
}
