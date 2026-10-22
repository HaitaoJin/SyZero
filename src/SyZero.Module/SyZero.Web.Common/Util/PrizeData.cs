namespace SyZero.Web.Common
{
    public class PrizeData
    {
        /// <summary>
        /// 奖品Id
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 奖品等级
        /// </summary>
        public int PrizeLevel { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// 概率
        /// </summary>
        public double Probability { get; set; }
    }
}
