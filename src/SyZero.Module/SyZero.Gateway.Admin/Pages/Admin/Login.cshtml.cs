using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SyZero.Gateway.Admin.Pages.Admin
{
    /// <summary>
    /// 登录页 PageModel；表单提交完全由前端走 <c>/admin-api/auth/login</c>。
    /// </summary>
    public sealed class LoginModel : PageModel
    {
        public string ReturnUrl { get; set; } = "/admin";

        public void OnGet(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl)
                && returnUrl.StartsWith('/')
                && !returnUrl.StartsWith("//"))
            {
                ReturnUrl = returnUrl;
            }
        }
    }
}
