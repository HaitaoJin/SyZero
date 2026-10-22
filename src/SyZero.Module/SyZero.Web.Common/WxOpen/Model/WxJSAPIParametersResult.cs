namespace SyZero.Web.Common
{
    public class WxJSAPIParametersResult : WxOpenJsonResult
    {
        public string ticket { get; set; } = string.Empty;

        public string noncestr { get; set; } = string.Empty;


        public string timestamp { get; set; } = string.Empty;

        public string signature { get; set; } = string.Empty;

        public string url { get; set; } = string.Empty;

    }
}
