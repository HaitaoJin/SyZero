using System.Collections.Generic;

namespace SyZero.Authorization.Permissions
{
    /// <summary>
    /// 权限定义（运行期内存对象）。
    /// </summary>
    public class PermissionDefinition
    {
        public string Name { get; }

        public string DisplayName { get; set; }

        public string? Description { get; set; }

        public string? GroupName { get; set; }

        public string? ParentName { get; set; }

        public int Sort { get; set; }

        public bool IsEnabled { get; set; } = true;

        internal List<PermissionDefinition> ChildrenInternal { get; } = new();

        public IReadOnlyList<PermissionDefinition> Children => ChildrenInternal;

        public PermissionDefinition(string name, string? displayName = null)
        {
            Name = name;
            DisplayName = displayName ?? name;
        }

        /// <summary>
        /// 添加子权限。
        /// </summary>
        public PermissionDefinition AddChild(string name, string? displayName = null)
        {
            var child = new PermissionDefinition(name, displayName)
            {
                GroupName = GroupName,
                ParentName = Name
            };
            ChildrenInternal.Add(child);
            return child;
        }
    }
}
