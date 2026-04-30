namespace SyZero.Web.Common
{
    public class WxacodeReturn
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public string errcode { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string errmsg { get; set; }

        /// <summary>
        /// 图片地址
        /// </summary>
        public string ImgBase64 { get; set; }
    }
}
