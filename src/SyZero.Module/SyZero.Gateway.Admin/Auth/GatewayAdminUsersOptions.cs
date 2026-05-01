#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 管理台多用户与角色配置。配置节：<see cref="SectionName"/>。
    /// </summary>
    /// <remarks>
    /// 支持两种 appsettings 配置形式（任选其一或并存，均视为内置账号）：
    /// <list type="number">
    ///   <item><description>单账号（兼容旧版）：<c>Gateway:Admin:Username</c> + <c>Gateway:Admin:Password</c>，默认角色 <c>Admin</c>。</description></item>
    ///   <item><description>多账号：<c>Gateway:Admin:Users</c> 数组，每项含 <c>Name</c> / <c>Password</c> / <c>Role</c>。</description></item>
    /// </list>
    /// 此外可在 <c>users.json</c> 中通过用户管理页运行时增删账号。所有内置账号 <see cref="GatewayAdminUser.IsBuiltIn"/>=true，不可在 UI 修改。
    /// </remarks>
    public sealed class GatewayAdminUsersOptions
    {
        public const string SectionName = "Gateway:Admin:Users";

        /// <summary>来自 appsettings 的内置账号（只读，无法在用户管理页修改）。</summary>
        public List<GatewayAdminUser> Users { get; set; } = new List<GatewayAdminUser>();
    }

    /// <summary>角色枚举（字符串）。</summary>
    public static class GatewayAdminRoles
    {
        public const string Admin = "Admin";
        public const string Reader = "Reader";

        public static bool IsValid(string? role)
            => string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Reader, StringComparison.OrdinalIgnoreCase);

        public static string Normalize(string? role)
            => string.Equals(role, Reader, StringComparison.OrdinalIgnoreCase) ? Reader : Admin;
    }

    /// <summary>管理台用户。</summary>
    public sealed class GatewayAdminUser
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>角色：<c>Admin</c> 或 <c>Reader</c>。默认 <c>Admin</c>。</summary>
        public string Role { get; set; } = GatewayAdminRoles.Admin;

        /// <summary>
        /// 是否为内置账号（来自 appsettings）。仅在运行时由 <c>IUserResolver</c> 填充，不参与序列化。
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; }
    }
}
