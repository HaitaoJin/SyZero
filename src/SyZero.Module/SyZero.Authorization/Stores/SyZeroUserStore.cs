using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SyZero.Authorization.Entities;
using SyZero.Dependency;
using SyZero.Domain.Repository;

namespace SyZero.Authorization.Stores
{
    /// <summary>
    /// 基于 SyZero <see cref="IRepository{TEntity}"/> 的 IdentityUser 存储实现，
    /// 实现 IUserPasswordStore + IUserEmailStore + IUserSecurityStampStore + IUserPhoneNumberStore + IUserLockoutStore。
    /// </summary>
    public class SyZeroUserStore :
        IUserPasswordStore<User>,
        IUserEmailStore<User>,
        IUserSecurityStampStore<User>,
        IUserPhoneNumberStore<User>,
        IUserLockoutStore<User>,
        IUserTwoFactorStore<User>,
        IScopedDependency
    {
        private readonly IRepository<User> _users;

        public SyZeroUserStore(IRepository<User> users)
        {
            _users = users;
        }

        public IQueryable<User> Users => _users.GetList();

        #region IUserStore
        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult<string?>(user.UserName);

        public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName ?? string.Empty;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult<string?>(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName ?? string.Empty;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            await _users.AddAsync(user, cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            await _users.UpdateAsync(user, cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            await _users.DeleteAsync(user.Id, cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (!long.TryParse(userId, out var id)) return null;
            return await _users.GetModelAsync(id, cancellationToken);
        }

        public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _users.GetModelAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
        }

        public void Dispose() { }
        #endregion

        #region IUserPasswordStore
        public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        #endregion

        #region IUserEmailStore
        public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.EmailConfirmed);

        public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => await _users.GetModelAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.NormalizedEmail);

        public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }
        #endregion

        #region IUserSecurityStampStore
        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.SecurityStamp);
        #endregion

        #region IUserPhoneNumberStore
        public Task SetPhoneNumberAsync(User user, string? phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        public Task<string?> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.PhoneNumber);

        public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.PhoneNumberConfirmed);

        public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }
        #endregion

        #region IUserLockoutStore
        public Task<System.DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.LockoutEnd);

        public Task SetLockoutEndDateAsync(User user, System.DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.AccessFailedCount);

        public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.LockoutEnabled);

        public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }
        #endregion

        #region IUserTwoFactorStore
        public Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken)
            => Task.FromResult(user.TwoFactorEnabled);
        #endregion
    }
}
