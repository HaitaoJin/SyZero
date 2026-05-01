namespace SyZero.Web.Common
{
    public class WxacodeInfoInput
    {
        /// <summary>
        /// 自定义参数
        /// </summary>
        public string scene { get; set; } = string.Empty;

        /// <summary>
        /// 页面地址
        /// </summary>
        public string page { get; set; } = string.Empty;

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int width { get; set; }
    }
}
