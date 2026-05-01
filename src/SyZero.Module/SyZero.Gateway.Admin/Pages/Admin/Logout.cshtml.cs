using Microsoft.AspNetCore.Mvc.RazorPages;
using SyZero.Gateway.Admin.Auth;

namespace SyZero.Gateway.Admin.Pages.Admin
{
    /// <summary>
    /// 登出页：清除登录 cookie 后由前端跳转登录页。
    /// </summary>
    public sealed class LogoutModel : PageModel
    {
        public void OnGet()
        {
            // 服务端兜底删除 cookie；前端还会再调用 /admin-api/auth/logout 双保险。
            Response.Cookies.Delete(AdminBasicAuthMiddleware.AuthCookieName);
        }
    }
}

