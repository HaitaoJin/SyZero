using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyZero.Authorization.Entities;
using SyZero.Domain.Repository;

namespace SyZero.Authorization.Permissions
{
    /// <summary>
    /// 启动时把 <see cref="IPermissionDefinitionProvider"/> 提供的权限定义同步到数据库。
    /// </summary>
    public class PermissionSyncHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PermissionSyncHostedService> _logger;

        public PermissionSyncHostedService(IServiceProvider serviceProvider, ILogger<PermissionSyncHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<PermissionDefinitionManager>();
            if (manager == null)
            {
                return;
            }

            var groupRepo = scope.ServiceProvider.GetService<IRepository<PermissionGroup>>();
            var permRepo = scope.ServiceProvider.GetService<IRepository<Permission>>();
            if (groupRepo == null || permRepo == null)
            {
                _logger.LogWarning("PermissionSync 跳过：未找到 IRepository<PermissionGroup/Permission>，请先注册仓储 Provider。");
                return;
            }

            try
            {
                foreach (var groupDef in manager.Groups)
                {
                    await UpsertGroupAsync(groupRepo, permRepo, groupDef, parentId: null, cancellationToken);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PermissionSync 同步失败");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task UpsertGroupAsync(
            IRepository<PermissionGroup> groupRepo,
            IRepository<Permission> permRepo,
            PermissionGroupDefinition def,
            long? parentId,
            CancellationToken cancellationToken)
        {
            var existing = groupRepo.GetList(g => g.Name == def.Name).FirstOrDefault();
            if (existing == null)
            {
                existing = new PermissionGroup
                {
                    Name = def.Name,
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    ParentId = parentId,
                    Sort = def.Sort
                };
                await groupRepo.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.DisplayName = def.DisplayName;
                existing.Description = def.Description;
                existing.ParentId = parentId;
                existing.Sort = def.Sort;
                await groupRepo.UpdateAsync(existing, cancellationToken);
            }

            foreach (var permDef in def.Permissions)
            {
                await UpsertPermissionAsync(permRepo, permDef, existing.Id, parentPermissionId: null, cancellationToken);
            }

            foreach (var child in def.Children)
            {
                await UpsertGroupAsync(groupRepo, permRepo, child, existing.Id, cancellationToken);
            }
        }

        private static async Task UpsertPermissionAsync(
            IRepository<Permission> permRepo,
            PermissionDefinition def,
            long? groupId,
            long? parentPermissionId,
            CancellationToken cancellationToken)
        {
            var existing = permRepo.GetList(p => p.Name == def.Name).FirstOrDefault();
            if (existing == null)
            {
                existing = new Permission
                {
                    Name = def.Name,
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    GroupId = groupId,
                    ParentId = parentPermissionId,
                    IsEnabled = def.IsEnabled,
                    Sort = def.Sort
                };
                await permRepo.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.DisplayName = def.DisplayName;
                existing.Description = def.Description;
                existing.GroupId = groupId;
                existing.ParentId = parentPermissionId;
                existing.IsEnabled = def.IsEnabled;
                existing.Sort = def.Sort;
                await permRepo.UpdateAsync(existing, cancellationToken);
            }

            foreach (var child in def.Children)
            {
                await UpsertPermissionAsync(permRepo, child, groupId, existing.Id, cancellationToken);
            }
        }
    }
}
