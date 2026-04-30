using System.Collections.Generic;

namespace SyZero.Application.Service.Dto
{
    public class PageResultDto<T> : ListResultDto<T>, IPageResult<T>, IListResult<T>
    {
        /// <summary>
        /// 数据总数
        /// </summary>
        public int total { get; set; }

        /// <summary>
        /// 返回分页数据
        /// </summary>
        /// <param name="totalCount">数据总数</param>
        /// <param name="items">列表数据</param>
        public PageResultDto(int totalCount, IList<T> items) : base(items)
        {
            this.total = totalCount;
        }
    }
}
