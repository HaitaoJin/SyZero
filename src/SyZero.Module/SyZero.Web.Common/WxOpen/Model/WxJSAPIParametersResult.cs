namespace SyZero.Web.Common
{
    public class WxJSAPIParametersResult : WxOpenJsonResult
    {
        public string ticket { get; set; }

        public string noncestr { get; set; }


        public string timestamp { get; set; }

        public string signature { get; set; }

        public string url { get; set; }

    }
}
