using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SyZero.Runtime.Security;
using System.Linq;
using Microsoft.IdentityModel.Logging;
using Microsoft.Extensions.Options;

namespace SyZero.Web.Common.Jwt
{
    public class JwtToken : IToken
    {
        private readonly JwtTokenOptions _options;

        public JwtToken(IOptions<JwtTokenOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
        }

        public string CreateAccessToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var authTime = DateTime.UtcNow;
            var expiresAt = authTime.AddDays(_options.Expires);
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? GetPrincipal(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler(); // 创建一个JwtSecurityTokenHandler类，用来后续操作
                var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken; // 将字符串token解码成token对象
                if (jwtToken == null)
                    return null;

                var validationParameters = new TokenValidationParameters() // 生成验证token的参数
                {
                    ValidateIssuer = true,//是否验证Issuer
                    ValidateAudience = true,//是否验证Audience
                    ValidateLifetime = true,//是否验证失效时间
                    ValidateIssuerSigningKey = true,//是否验证SecurityKey
                    ValidAudience = _options.Audience,
                    ValidIssuer = _options.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityKey))
                };
                SecurityToken securityToken; // 接受解码后的token对象
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
                claimsPrincipal.Identities.First().AddClaim(new Claim(SyClaimTypes.Token, token));
                return claimsPrincipal;
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class JwtTokenOptions
    {
        public const string SectionName = "JWT";

        public string SecurityKey { get; set; } = string.Empty;
        public int Expires { get; set; } = 1;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        public void Validate()
        {
            if (Encoding.UTF8.GetByteCount(SecurityKey) < 32)
            {
                throw new InvalidOperationException("JWT:SecurityKey 必须至少为 32 字节。");
            }

            if (Expires <= 0 || string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            {
                throw new InvalidOperationException("JWT 配置中的 Expires、Issuer 和 Audience 必须有效。");
            }
        }
    }
}
