namespace SyZero.Authorization.Jwt
{
    /// <summary>
    /// JWT 配置选项。
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// 签发者
        /// </summary>
        public string Issuer { get; set; } = "SyZero";

        /// <summary>
        /// 受众
        /// </summary>
        public string Audience { get; set; } = "SyZero";

        /// <summary>
        /// 对称密钥（HMAC SHA256）。生产环境务必使用强随机长字符串。
        /// </summary>
        public string SecretKey { get; set; } = "syzero-default-secret-please-change-me-32+chars";

        /// <summary>
        /// 访问令牌过期时间（分钟）
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 刷新令牌过期时间（天）
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
