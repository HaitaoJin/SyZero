namespace SyZero.Web.Common
{
    public class WxAccessToken
    {
        /// <summary>
        /// 微信返回的小程序凭证
        /// </summary>
        public string access_token { get; set; } = string.Empty;

        /// <summary>
        /// 返回代码
        /// </summary>
        public string expires_in { get; set; } = string.Empty;
    }
}
