namespace SyZero.Web.Common
{
    /// <summary>
    /// 微信用户信息
    /// </summary>
    public class WxUserInfoResult : WxOpenJsonResult
    {
        /// <summary>
        /// 用户是否订阅该公众号标识，值为0时，代表此用户没有关注该公众号，拉取不到其余信息
        /// </summary>
        public int subscribe { get; set; }
        /// <summary>
        /// 微信OpenId
        /// </summary>
        public string openid { get; set; }
        /// <summary>
        /// 普通用户昵称
        /// </summary>
        public string nickname { get; set; }
        /// <summary>
        /// 普通用户性别，1为男性，2为女性
        /// </summary>
        public int sex { get; set; }
        /// <summary>
        /// 普通用户个人资料填写的省份
        /// </summary>
        public string province { get; set; }
        /// <summary>
        /// 	普通用户个人资料填写的城市
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// 国家，如中国为CN
        /// </summary>
        public string country { get; set; }
        /// <summary>
        /// 用户头像，最后一个数值代表正方形头像大小（有0、46、64、96、132数值可选，0代表640*640正方形头像），用户没有头像时该项为空
        /// </summary>
        public string headimgurl { get; set; }
        /// <summary>
        /// 用户统一标识。针对一个微信开放平台帐号下的应用，同一用户的unionid是唯一的。
        /// </summary>
        public string unionid { get; set; }
    }
}
