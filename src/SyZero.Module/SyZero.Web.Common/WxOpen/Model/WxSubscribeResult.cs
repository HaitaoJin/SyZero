namespace SyZero.Web.Common
{
    public class WxSubscribeResult
    {
        /// <summary>
        /// 接受者openId
        /// </summary>
        public string touser { get; set; }
        /// <summary>
        /// 模板Id
        /// </summary>
        public string template_id { get; set; }
        /// <summary>
        /// 模板内容
        /// </summary>
        public object data { get; set; }


    }
}
