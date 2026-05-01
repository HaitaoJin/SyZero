using System.Collections.Generic;

namespace SyZero.Application.Service.Dto
{
    public class ListResultDto<T> : IListResult<T>
    {
        public IList<T> list { get; set; } = new List<T>();

        /// <summary>
        /// 返回列表数据
        /// </summary>
        public ListResultDto()
        {

        }

        /// <summary>
        /// 返回列表数据
        /// </summary>
        /// <param name="items">列表数据</param>
        public ListResultDto(IList<T> items)
        {
            list = items;
        }
    }
}
