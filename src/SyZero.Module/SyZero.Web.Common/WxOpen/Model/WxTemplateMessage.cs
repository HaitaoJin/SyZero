namespace SyZero.Web.Common
{
    /// <summary>
    /// 微信模板
    /// </summary>
    public class WxTemplateMessage
    {
        /// <summary>
        /// 接收者openid 必填
        /// </summary>
        public string touser { get; set; }
        /// <summary>
        /// 模板ID 必填
        /// </summary>
        public string template_id { get; set; }
        /// <summary>
        /// 模板跳转链接（海外帐号没有跳转能力）
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 跳小程序所需数据，不需跳小程序可不用传该数据
        /// </summary>
        public Miniprogram miniprogram { get; set; }
        /// <summary>
        /// 模板数据 必填
        /// </summary>
        public TemplateMessageData data { get; set; }
    }


    public class Miniprogram
    {
        public Miniprogram(string appid, string pagepath)
        {
            this.appid = appid;
            this.pagepath = pagepath;
        }

        public Miniprogram()
        {

        }
        /// <summary>
        /// 所需跳转到的小程序appid（该小程序appid必须与发模板消息的公众号是绑定关联关系，暂不支持小游戏）
        /// </summary>
        public string appid { get; set; }
        /// <summary>
        /// 所需跳转到小程序的具体页面路径，支持带参数,（示例index?foo=bar），要求该小程序已发布，暂不支持小游戏
        /// </summary>
        public string pagepath { get; set; }
    }

    public class TemplateMessageData
    {
        public TemplateMessageDataValue first { get; set; }

        public TemplateMessageDataValue keyword1 { get; set; }

        public TemplateMessageDataValue keyword2 { get; set; }

        public TemplateMessageDataValue keyword3 { get; set; }

        public TemplateMessageDataValue keyword4 { get; set; }

        public TemplateMessageDataValue keyword5 { get; set; }

        public TemplateMessageDataValue remark { get; set; }
    }

    public class TemplateMessageDataValue
    {

        public TemplateMessageDataValue(string value, string color)
        {
            this.value = value;
            this.color = color;
        }

        public TemplateMessageDataValue(string value)
        {
            this.value = value;
        }

        public TemplateMessageDataValue()
        {

        }

        /// <summary>
        /// 模板内容 必填
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// 模板内容字体颜色，不填默认为黑色
        /// </summary>
        public string color { get; set; }
    }
}
