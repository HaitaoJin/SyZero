using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SyZero.Authorization.Entities;
using SyZero.Dependency;
using SyZero.Domain.Repository;
using SyZero.Runtime.Security;

namespace SyZero.Authorization.Jwt
{
    /// <summary>
    /// 默认 RefreshToken 实现：32 字节加密随机 + Base64Url；DB 仅存 SHA-256 哈希。
    /// </summary>
    public class RefreshTokenManager : IRefreshTokenManager, IScopedDependency
    {
        private readonly IRepository<RefreshToken> _repo;
        private readonly IToken _accessTokenIssuer;
        private readonly JwtOptions _options;

        public RefreshTokenManager(
            IRepository<RefreshToken> repo,
            IToken accessTokenIssuer,
            IOptions<JwtOptions> options)
        {
            _repo = repo;
            _accessTokenIssuer = accessTokenIssuer;
            _options = options.Value;
        }

        public async Task<TokenPairResult> IssueAsync(long userId, IEnumerable<Claim> accessClaims, string? createdByIp = null, CancellationToken ct = default)
        {
            var (raw, hash) = GenerateToken();
            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = hash,
                ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays),
                CreatedByIp = createdByIp
            };
            await _repo.AddAsync(entity, ct);

            return new TokenPairResult
            {
                AccessToken = _accessTokenIssuer.CreateAccessToken(accessClaims),
                RefreshToken = raw,
                UserId = userId
            };
        }

        public async Task<TokenPairResult> RotateAsync(string refreshToken, IEnumerable<Claim> newAccessClaims, string? createdByIp = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new SyMessageException("refresh_token 不能为空");
            }

            var hash = Hash(refreshToken);
            var existing = _repo.GetList(t => t.TokenHash == hash).FirstOrDefault()
                ?? throw new SyMessageException("无效的 refresh_token");

            // 检测旧 token 复用 → 安全事件，级联撤销
            if (existing.RevokedAt != null)
            {
                await RevokeAllAsync(existing.UserId, ct);
                throw new SyMessageException("refresh_token 已被撤销，已级联撤销账户全部令牌");
            }

            if (DateTime.UtcNow >= existing.ExpiresAt)
            {
                throw new SyMessageException("refresh_token 已过期");
            }

            var (newRaw, newHash) = GenerateToken();
            var newEntity = new RefreshToken
            {
                UserId = existing.UserId,
                TokenHash = newHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays),
                CreatedByIp = createdByIp
            };
            await _repo.AddAsync(newEntity, ct);

            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByTokenHash = newHash;
            await _repo.UpdateAsync(existing, ct);

            return new TokenPairResult
            {
                AccessToken = _accessTokenIssuer.CreateAccessToken(newAccessClaims),
                RefreshToken = newRaw,
                UserId = existing.UserId
            };
        }

        public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
        {
            var hash = Hash(refreshToken);
            var existing = _repo.GetList(t => t.TokenHash == hash).FirstOrDefault();
            if (existing == null || existing.RevokedAt != null) return;
            existing.RevokedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(existing, ct);
        }

        public async Task RevokeAllAsync(long userId, CancellationToken ct = default)
        {
            var actives = _repo.GetList(t => t.UserId == userId && t.RevokedAt == null).ToList();
            var now = DateTime.UtcNow;
            foreach (var t in actives)
            {
                t.RevokedAt = now;
                await _repo.UpdateAsync(t, ct);
            }
        }

        private static (string Raw, string Hash) GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var raw = Base64UrlEncode(bytes);
            var hash = Hash(raw);
            return (raw, hash);
        }

        private static string Hash(string raw)
        {
            var bytes = Encoding.UTF8.GetBytes(raw);
            var sha = SHA256.HashData(bytes);
            return Base64UrlEncode(sha);
        }

        private static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
