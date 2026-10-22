namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 登录限流可配置项，绑定到配置节 <c>Gateway:Admin:RateLimit</c>。
    /// 任意值 &lt;= 0 时退回到 <see cref="Defaults"/> 中的默认值。
    /// </summary>
    public sealed class GatewayAdminRateLimitOptions
    {
        public const string SectionName = "Gateway:Admin:RateLimit";

        /// <summary>触发锁定前允许的最大失败次数（默认 5）。</summary>
        public int MaxFailures { get; set; } = Defaults.MaxFailures;

        /// <summary>锁定持续分钟数（默认 5）。</summary>
        public int LockoutMinutes { get; set; } = Defaults.LockoutMinutes;

        /// <summary>失败计数累计窗口分钟数；窗口结束（且未锁定）则计数清零（默认 15）。</summary>
        public int WindowMinutes { get; set; } = Defaults.WindowMinutes;

        /// <summary>是否信任 <see cref="ForwardedHeaderName"/> 提供的客户端 IP（位于反向代理后才应启用，默认 false）。</summary>
        public bool TrustForwardedFor { get; set; } = false;

        /// <summary>承载真实客户端 IP 的请求头名称（默认 <c>X-Forwarded-For</c>）。仅 <see cref="TrustForwardedFor"/> 为 true 时生效。</summary>
        public string ForwardedHeaderName { get; set; } = "X-Forwarded-For";

        /// <summary>触发验证码的失败次数阈值（默认 3）。设为 <c>0</c> 时关闭验证码。当当前失败次数 ≥ 该值时，登录请求必须携带 <c>CaptchaId</c> + <c>CaptchaAnswer</c>。</summary>
        public int CaptchaThreshold { get; set; } = Defaults.CaptchaThreshold;

        public static class Defaults
        {
            public const int MaxFailures = 5;
            public const int LockoutMinutes = 5;
            public const int WindowMinutes = 15;
            public const int CaptchaThreshold = 3;
        }

        internal int EffectiveMaxFailures => MaxFailures > 0 ? MaxFailures : Defaults.MaxFailures;
        internal int EffectiveLockoutMinutes => LockoutMinutes > 0 ? LockoutMinutes : Defaults.LockoutMinutes;
        internal int EffectiveWindowMinutes => WindowMinutes > 0 ? WindowMinutes : Defaults.WindowMinutes;
        /// <summary>有效阈值；&lt;0 视为 0（关闭）。</summary>
        internal int EffectiveCaptchaThreshold => CaptchaThreshold < 0 ? 0 : CaptchaThreshold;
    }
}
