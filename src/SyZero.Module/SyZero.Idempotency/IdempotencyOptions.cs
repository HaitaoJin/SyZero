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

        /// <summary>同一个幂等键对应不同请求内容时返回的状态码。</summary>
        public int ConflictStatusCode { get; set; } = 422;

        /// <summary>允许缓存的最大响应体字节数，默认 1 MiB。</summary>
        public int MaxResponseBodyBytes { get; set; } = 1024 * 1024;

        /// <summary>计算请求指纹时读取的最大请求体字节数，默认 1 MiB。</summary>
        public int MaxRequestBodyBytes { get; set; } = 1024 * 1024;

        /// <summary>将用户、租户、HTTP 方法与路径加入存储键，避免跨调用方冲突。</summary>
        public bool ScopeKeyToRequest { get; set; } = true;

        /// <summary>
        /// 仅对这些 HTTP 方法生效（默认 POST / PUT / PATCH / DELETE；GET / HEAD 默认幂等）。
        /// </summary>
        public string[] Methods { get; set; } = new[] { "POST", "PUT", "PATCH", "DELETE" };
    }
}
