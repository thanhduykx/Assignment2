using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Account;

[AllowAnonymous]
public sealed class RegisterModel : PageModel
{
    private const string AccountProvisioningMessage = "Tài khoản được cấp bởi Nhà trường. Vui lòng liên hệ Nhà trường để xin cấp tài khoản.";

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        TempData["AuthError"] = AccountProvisioningMessage;
        return RedirectToPage("/Account/Login");
    }

    public IActionResult OnPost()
    {
        TempData["AuthError"] = AccountProvisioningMessage;
        return RedirectToPage("/Account/Login");
    }
}
