namespace SyZero.Idempotency
{
    /// <summary>
    /// 幂等中间件配置。
    /// </summary>
    public class IdempotencyOptions
    {
        public const string SectionName = "SyZeroIdempotency";

        /// <summary>
        /// 请求头名称，默认 <c>Idempotency-Key</c>。
        /// </summary>
        public string HeaderName { get; set; } = "Idempotency-Key";

        /// <summary>
        /// key 的过期时间（秒），默认 24 小时。
        /// </summary>
        public int ExpirationSeconds { get; set; } = 24 * 60 * 60;

        /// <summary>
        /// 当首个请求仍在处理（已占位但未保存）时，并发请求返回的状态码。
        /// </summary>
        public int InflightRejectionStatusCode { get; set; } = 409;

        /// <summary>
        /// 仅对这些 HTTP 方法生效（默认 POST / PUT / PATCH / DELETE；GET / HEAD 默认幂等）。
        /// </summary>
        public string[] Methods { get; set; } = new[] { "POST", "PUT", "PATCH", "DELETE" };
    }
}
