using System;
using System.Collections.Generic;
using System.Linq;

namespace SyZero.Web.Common
{
    public class PrizeUtil : IPrizeUtil
    {
        public List<PrizeData> ReRatePrize(List<PrizeData> old, int indextarget)
        {
            var oldquantity = new List<PrizeData>();
            if (indextarget != -1)
            {
                var target = old[indextarget];
                var targetRate = 1 / Convert.ToDouble(target.Quantity) * target.Probability;
                target.Quantity -= 1;
                if (target.Quantity <= 0)
                {
                    old.Remove(target);
                }
                old.Where(y => y != target).ToList().ForEach(x => { oldquantity.Add(new PrizeData() { PrizeLevel = x.PrizeLevel, Id = x.Id, Quantity = x.Quantity }); });
                if (oldquantity.Any())
                {
                    var quantityValues = oldquantity.Select(x => Convert.ToDouble(x.Quantity)).ToList();
                    var separateVaule = GetSeparateVaule(quantityValues);
                    old.ForEach(x =>
                    {
                        if (x != target)
                        {
                            x.Probability = x.Probability + ReRateValue(Convert.ToDouble(x.Quantity), quantityValues, separateVaule, targetRate);
                        }
                        else
                        {
                            x.Probability = x.Probability - targetRate;
                        }
                    });
                }
            }
            return old;
        }

        /// <summary>
        /// 重分配权重(当前)
        /// </summary>
        /// <param name="thisQuantity"></param>
        /// <param name="allQuantity"></param>
        /// <param name="separateVaule"></param>
        /// <param name="targetRate"></param>
        /// <returns></returns>
        private double ReRateValue(double thisQuantity, List<double> allQuantity, double separateVaule, double targetRate)
        {
            return (GetValueForAll(thisQuantity, allQuantity) * GetValueForMax(thisQuantity, allQuantity) + separateVaule * GetValueForAll(thisQuantity, allQuantity)) * targetRate;
        }
        /// <summary>
        /// 低权重降权
        /// </summary>
        /// <param name="allQuantity"></param>
        /// <returns></returns>
        private double GetSeparateVaule(List<double> allQuantity)
        {
            return allQuantity.Sum(x => GetValueForAll(x, allQuantity) - GetValueForAll(x, allQuantity) * GetValueForMax(x, allQuantity));
        }
        /// <summary>
        /// 获取权重比(所有)
        /// </summary>
        /// <param name="thisQuantity"></param>
        /// <param name="allQuantity"></param>
        /// <returns></returns>
        private double GetValueForAll(double thisQuantity, List<double> allQuantity)
        {
            return thisQuantity / allQuantity.Sum();
        }
        /// <summary>
        /// 获取权重比(最大)
        /// </summary>
        /// <param name="thisQuantity"></param>
        /// <param name="allQuantity"></param>
        /// <returns></returns>
        private static double GetValueForMax(double thisQuantity, List<double> allQuantity)
        {
            return thisQuantity / allQuantity.Max();
        }
    }
}
