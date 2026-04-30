using System.Security.Claims;

namespace SyZero.Runtime.Security
{
    /// <summary>
    /// 身份信息
    /// </summary>
    public static class SyClaimTypes
    {
        /// <summary>
        /// System.
        /// Default: <see cref="System"/>
        /// </summary>
        public static string System { get; set; } = "system";

        /// <summary>
        /// UserName.
        /// Default: <see cref="ClaimTypes.Name"/>
        /// </summary>
        public static string UserName { get; set; } = "userName";

        /// <summary>
        /// NickName
        /// Default: <see cref="NickName"/>
        /// </summary>
        public static string NickName { get; set; } = "nickName";

        /// <summary>
        /// UserId.
        /// Default: <see cref="ClaimTypes.NameIdentifier"/>
        /// </summary>
        public static string UserId { get; set; } = "userId";

        /// <summary>
        /// UserRole.
        /// Default: <see cref="ClaimTypes.Role"/>
        /// </summary>
        public static string UserRole { get; set; } = "userRole";

        /// <summary>
        /// Permission.
        /// Default: <see cref="ClaimTypes.Authentication"/>
        /// </summary>
        public static string Permission { get; set; } = "permission";

        /// <summary>
        /// AvatarUrl.
        /// Default: <see cref="AvatarUrl"/>
        /// </summary>
        public static string AvatarUrl { get; set; } = "avatarUrl";

        /// <summary>
        /// Token.
        /// Default: <see cref="Token"/>
        /// </summary>
        public static string Token { get; set; } = "token";
    }
}
