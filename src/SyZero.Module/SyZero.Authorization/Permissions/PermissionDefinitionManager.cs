using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Dependency;

namespace SyZero.Authorization.Permissions
{
    /// <summary>
    /// 权限定义管理器，负责在启动时聚合所有 <see cref="IPermissionDefinitionProvider"/> 的定义。
    /// </summary>
    public class PermissionDefinitionManager : ISingletonDependency
    {
        private readonly Dictionary<string, PermissionDefinition> _permissions = new();
        private readonly List<PermissionGroupDefinition> _groups = new();
        private bool _initialized;
        private readonly object _lock = new();
        private readonly IServiceProvider _serviceProvider;

        public PermissionDefinitionManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 全部权限定义（扁平）。
        /// </summary>
        public IReadOnlyDictionary<string, PermissionDefinition> Permissions
        {
            get
            {
                EnsureInitialized();
                return _permissions;
            }
        }

        /// <summary>
        /// 顶层权限组。
        /// </summary>
        public IReadOnlyList<PermissionGroupDefinition> Groups
        {
            get
            {
                EnsureInitialized();
                return _groups;
            }
        }

        /// <summary>
        /// 按名称获取权限定义。
        /// </summary>
        public PermissionDefinition? Get(string name)
        {
            EnsureInitialized();
            return _permissions.TryGetValue(name, out var def) ? def : null;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;

                var providers = _serviceProvider.GetServices<IPermissionDefinitionProvider>();
                var context = new PermissionDefinitionContext();
                foreach (var provider in providers)
                {
                    provider.Define(context);
                }

                foreach (var group in context.Groups)
                {
                    _groups.Add(group);
                    CollectPermissions(group.Permissions);
                    foreach (var child in group.Children)
                    {
                        CollectGroup(child);
                    }
                }

                _initialized = true;
            }
        }

        private void CollectGroup(PermissionGroupDefinition group)
        {
            CollectPermissions(group.Permissions);
            foreach (var child in group.Children)
            {
                CollectGroup(child);
            }
        }

        private void CollectPermissions(IEnumerable<PermissionDefinition> permissions)
        {
            foreach (var p in permissions)
            {
                _permissions[p.Name] = p;
                CollectPermissions(p.Children);
            }
        }
    }
}
