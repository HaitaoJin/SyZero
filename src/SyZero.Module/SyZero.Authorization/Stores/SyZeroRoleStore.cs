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
    /// 基于 SyZero <see cref="IRepository{TEntity}"/> 的 IdentityRole 存储实现。
    /// </summary>
    public class SyZeroRoleStore : IRoleStore<Role>, IScopedDependency
    {
        private readonly IRepository<Role> _roles;

        public SyZeroRoleStore(IRepository<Role> roles)
        {
            _roles = roles;
        }

        public IQueryable<Role> Roles => _roles.GetList();

        public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            await _roles.AddAsync(role, cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            await _roles.UpdateAsync(role, cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            await _roles.DeleteAsync(role.Id, cancellationToken);
            return IdentityResult.Success;
        }

        public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
            => Task.FromResult(role.Id.ToString());

        public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
            => Task.FromResult<string?>(role.Name);

        public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName ?? string.Empty;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
            => Task.FromResult<string?>(role.NormalizedName);

        public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName ?? string.Empty;
            return Task.CompletedTask;
        }

        public async Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            if (!long.TryParse(roleId, out var id)) return null;
            return await _roles.GetModelAsync(id, cancellationToken);
        }

        public async Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
            => await _roles.GetModelAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);

        public void Dispose() { }
    }
}
