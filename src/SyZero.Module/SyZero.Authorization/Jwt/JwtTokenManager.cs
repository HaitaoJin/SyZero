using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SyZero.Dependency;
using SyZero.Runtime.Security;

namespace SyZero.Authorization.Jwt
{
    /// <summary>
    /// 默认基于 HMAC-SHA256 的 JWT 实现。
    /// </summary>
    public class JwtTokenManager : IToken, ISingletonDependency
    {
        private readonly JwtOptions _options;
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtTokenManager(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public ClaimsPrincipal? GetPrincipal(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                var validation = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = GetSigningKey(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                return _handler.ValidateToken(token, validation, out _);
            }
            catch
            {
                return null;
            }
        }

        public string CreateAccessToken(IEnumerable<Claim> claims)
        {
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_options.AccessTokenExpirationMinutes),
                signingCredentials: new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256));
            return _handler.WriteToken(jwt);
        }

        private SymmetricSecurityKey GetSigningKey()
            => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
    }
}
