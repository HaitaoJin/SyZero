using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SyZero.Gateway.Admin.Pages.Admin
{
    /// <summary>
    /// 用户管理页面：列出 appsettings 内置账号 + users.json 文件账号；
    /// Admin 角色可对 file 账号 CRUD，任何登录用户可改自己的密码。
    /// </summary>
    public sealed class UsersModel : PageModel
    {
    }
}
