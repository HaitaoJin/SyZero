namespace SyZero.Runtime.Security
{
    /// <summary>
    /// 权限检查器抽象。框架内置默认实现 <see cref="DefaultPermissionChecker"/>，
    /// 业务可通过依赖注入替换为自定义实现（如对接 ASP.NET Core Identity / RBAC 数据库）。
    /// </summary>
    public interface IPermissionChecker
    {
        /// <summary>
        /// 是否拥有指定权限。
        /// </summary>
        bool IsHas(string permission);

        /// <summary>
        /// 检查权限，无权限时抛出 <see cref="SyMessageException"/>。
        /// </summary>
        void Authorize(string permission);
    }
}
