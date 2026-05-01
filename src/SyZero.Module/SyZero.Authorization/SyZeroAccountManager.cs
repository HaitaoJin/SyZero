using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SyZero.Authorization.Entities;
using SyZero.Authorization.Jwt;
using SyZero.Dependency;

namespace SyZero.Authorization
{
    /// <summary>
    /// 账户便捷操作粘合层：登出 / 全终端登出 / 改密（自动撤销全部刷新令牌）。
    /// 业务侧推荐注入此类而非直接组合 <see cref="UserManager{TUser}"/> + <see cref="IRefreshTokenManager"/>。
    /// </summary>
    public class SyZeroAccountManager : IScopedDependency
    {
        private readonly UserManager<User> _userManager;
        private readonly IRefreshTokenManager _refresh;

        public SyZeroAccountManager(UserManager<User> userManager, IRefreshTokenManager refresh)
        {
            _userManager = userManager;
            _refresh = refresh;
        }

        /// <summary>
        /// 单终端登出：仅撤销当前 RefreshToken。
        /// </summary>
        public Task LogoutAsync(string refreshToken, CancellationToken ct = default)
            => _refresh.RevokeAsync(refreshToken, ct);

        /// <summary>
        /// 全终端登出：撤销该用户全部活跃 RefreshToken。
        /// </summary>
        public Task LogoutAllAsync(long userId, CancellationToken ct = default)
            => _refresh.RevokeAllAsync(userId, ct);

        /// <summary>
        /// 改密：通过旧密码校验，成功后强制全终端登出。
        /// </summary>
        public async Task<IdentityResult> ChangePasswordAsync(long userId, string currentPassword, string newPassword, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _refresh.RevokeAllAsync(userId, ct);
            }
            return result;
        }

        /// <summary>
        /// 重置密码（管理员场景，无需旧密码）：重置后强制全终端登出。
        /// </summary>
        public async Task<IdentityResult> ResetPasswordAsync(long userId, string newPassword, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "用户不存在" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                await _refresh.RevokeAllAsync(userId, ct);
            }
            return result;
        }
    }
}
