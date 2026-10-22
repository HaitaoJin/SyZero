using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Authorization.Jwt
{
    /// <summary>
    /// 刷新令牌（Refresh Token）管理。
    /// 安全约定：
    /// - 原始 token 仅在签发/轮转时返回给调用方，DB 仅存 SHA-256 哈希；
    /// - 轮转链通过 <see cref="Entities.RefreshToken.ReplacedByTokenHash"/> 串联；
    /// - 已撤销 token 再次使用 → 视为泄漏，自动级联撤销该用户全部活跃 token。
    /// </summary>
    public interface IRefreshTokenManager
    {
        /// <summary>
        /// 为用户签发新的 RefreshToken（同时返回 access_token + refresh_token）。
        /// </summary>
        Task<TokenPairResult> IssueAsync(long userId, IEnumerable<Claim> accessClaims, string? createdByIp = null, CancellationToken ct = default);

        /// <summary>
        /// 用 RefreshToken 兑换新一对 token（旧 token 失效，写入轮转链）。
        /// </summary>
        Task<TokenPairResult> RotateAsync(string refreshToken, IEnumerable<Claim> newAccessClaims, string? createdByIp = null, CancellationToken ct = default);

        /// <summary>
        /// 撤销单个 RefreshToken。
        /// </summary>
        Task RevokeAsync(string refreshToken, CancellationToken ct = default);

        /// <summary>
        /// 撤销指定用户的全部活跃 RefreshToken（如修改密码后）。
        /// </summary>
        Task RevokeAllAsync(long userId, CancellationToken ct = default);
    }

    public sealed class TokenPairResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public long UserId { get; set; }
    }
}
