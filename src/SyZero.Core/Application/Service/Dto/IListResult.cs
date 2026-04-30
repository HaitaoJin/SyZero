using System.Collections.Generic;

namespace SyZero.Application.Service.Dto
{
    public interface IListResult<T>
    {
        /// <summary>
        /// 数据
        /// </summary>
        IList<T> list { get; set; }
    }
}
