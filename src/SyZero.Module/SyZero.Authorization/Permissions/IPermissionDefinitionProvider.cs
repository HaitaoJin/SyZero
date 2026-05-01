using System.Collections.Generic;

namespace SyZero.Authorization.Permissions
{
    /// <summary>
    /// 权限定义上下文，由 <see cref="IPermissionDefinitionProvider"/> 实现类填充。
    /// </summary>
    public class PermissionDefinitionContext
    {
        private readonly List<PermissionGroupDefinition> _groups = new();

        public IReadOnlyList<PermissionGroupDefinition> Groups => _groups;

        /// <summary>
        /// 新增一个顶层权限组。
        /// </summary>
        public PermissionGroupDefinition AddGroup(string name, string? displayName = null)
        {
            var group = new PermissionGroupDefinition(name, displayName);
            _groups.Add(group);
            return group;
        }
    }

    /// <summary>
    /// 权限定义提供者。模块通过实现此接口注入权限。
    /// </summary>
    public interface IPermissionDefinitionProvider
    {
        /// <summary>
        /// 定义权限。
        /// </summary>
        void Define(PermissionDefinitionContext context);
    }
}
