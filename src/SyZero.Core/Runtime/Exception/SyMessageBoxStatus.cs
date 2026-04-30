using System.ComponentModel;

namespace SyZero
{
    /// <summary>
    /// 消息状态
    /// </summary>
    public enum SyMessageBoxStatus
    {
        /// <summary>
        /// 无权限
        /// </summary>
        [Description("无权限")]
        NoPermission = -4,
        /// <summary>
        /// 未登录
        /// </summary>
        [Description("未登录")]
        UnAuthorized = -3,
        /// <summary>
        /// 服务端异常
        /// </summary>
        [Description("服务端异常")]
        Abnormal = -2,
        /// <summary>
        /// 自定义
        /// </summary>
        [Description("自定义")]
        Custom = -1,
        /// <summary>
        /// 失败
        /// </summary>
        [Description("成功")]
        Fail = 0,
        /// <summary>
        /// 成功
        /// </summary>
        [Description("成功")]
        Success = 1,
    }
}
