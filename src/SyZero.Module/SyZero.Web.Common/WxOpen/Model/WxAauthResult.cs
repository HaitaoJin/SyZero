namespace SyZero.Web.Common
{
    public class WxAauthResult : WxOpenJsonResult
    {
        public string access_token { get; set; } = string.Empty;

        public int expires_in { get; set; }

        public string refresh_token { get; set; } = string.Empty;

        public string openid { get; set; } = string.Empty;

        public string scope { get; set; } = string.Empty;
    }
}
