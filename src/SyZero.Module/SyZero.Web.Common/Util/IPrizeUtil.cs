using System.Collections.Generic;

namespace SyZero.Web.Common
{
    public interface IPrizeUtil
    {
        /// <summary>
        /// 重新分配权重(所有)
        /// </summary>
        /// <param name="old"></param>
        /// <param name="indextarget"></param>
        /// <returns></returns>
        List<PrizeData> ReRatePrize(List<PrizeData> old, int indextarget);
    }
}
