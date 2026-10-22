using System.Collections.Generic;

namespace SyZero.Authorization.Permissions
{
    /// <summary>
    /// 权限组定义（运行期内存对象）。
    /// </summary>
    public class PermissionGroupDefinition
    {
        public string Name { get; }

        public string DisplayName { get; set; }

        public string? Description { get; set; }

        public int Sort { get; set; }

        internal List<PermissionDefinition> PermissionsInternal { get; } = new();

        internal List<PermissionGroupDefinition> ChildrenInternal { get; } = new();

        public IReadOnlyList<PermissionDefinition> Permissions => PermissionsInternal;

        public IReadOnlyList<PermissionGroupDefinition> Children => ChildrenInternal;

        public PermissionGroupDefinition(string name, string? displayName = null)
        {
            Name = name;
            DisplayName = displayName ?? name;
        }

        /// <summary>
        /// 添加子权限组。
        /// </summary>
        public PermissionGroupDefinition AddGroup(string name, string? displayName = null)
        {
            var group = new PermissionGroupDefinition(name, displayName);
            ChildrenInternal.Add(group);
            return group;
        }

        /// <summary>
        /// 在当前组下添加权限。
        /// </summary>
        public PermissionDefinition AddPermission(string name, string? displayName = null)
        {
            var permission = new PermissionDefinition(name, displayName)
            {
                GroupName = Name
            };
            PermissionsInternal.Add(permission);
            return permission;
        }
    }
}
