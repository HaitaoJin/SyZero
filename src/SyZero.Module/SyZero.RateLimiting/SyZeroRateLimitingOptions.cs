namespace SyZero.RateLimiting
{
    /// <summary>
    /// SyZero.RateLimiting 配置选项。
    /// </summary>
    public class SyZeroRateLimitingOptions
    {
        /// <summary>
        /// 配置节名称。
        /// </summary>
        public const string SectionName = "SyZeroRateLimiting";

        /// <summary>
        /// 是否启用全局 ASP.NET Core RateLimiter 中间件，默认 true。
        /// </summary>
        public bool EnableGlobal { get; set; } = true;

        /// <summary>
        /// 全局策略名称，默认 "syzero-global"。
        /// </summary>
        public string GlobalPolicyName { get; set; } = "syzero-global";

        /// <summary>
        /// 全局每窗口允许的请求数，默认 100。
        /// </summary>
        public int GlobalPermitLimit { get; set; } = 100;

        /// <summary>
        /// 全局窗口长度（秒），默认 60。
        /// </summary>
        public int GlobalWindowSeconds { get; set; } = 60;

        /// <summary>
        /// 拒绝时返回的 HTTP 状态码，默认 429。
        /// </summary>
        public int RejectionStatusCode { get; set; } = 429;
    }
}
