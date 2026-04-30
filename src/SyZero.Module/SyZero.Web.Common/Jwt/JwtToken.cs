using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SyZero.Runtime.Security;
using System.Linq;
using Microsoft.IdentityModel.Logging;

namespace SyZero.Web.Common.Jwt
{
    public class JwtToken : IToken
    {
        public string CreateAccessToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppConfig.GetSection("JWT:SecurityKey")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var authTime = DateTime.UtcNow;
            var expiresAt = authTime.AddDays(int.Parse(AppConfig.GetSection("JWT:expires")));
            var token = new JwtSecurityToken(
                issuer: AppConfig.GetSection("JWT:issuer"),
                audience: AppConfig.GetSection("JWT:audience"),
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal GetPrincipal(string token)
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
                    ValidAudience = AppConfig.GetSection("JWT:audience"),//Audience
                    ValidIssuer = AppConfig.GetSection("JWT:issuer"),//Issuer，这两项和前面签发jwt的设置一致
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppConfig.GetSection("JWT:SecurityKey")))//拿到SecurityKey
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
}
