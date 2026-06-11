using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Services;

namespace PresentationLayer.Pages.Account;

[AllowAnonymous]
public sealed class ResetPasswordModel : PageModel
{
    private readonly IUserAccountStore _users;

    public ResetPasswordModel(IUserAccountStore users)
    {
        _users = users;
    }

    [BindProperty]
    public ResetPasswordViewModel Input { get; set; } = new();

    public IActionResult OnGet(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["AuthError"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return RedirectToPage("/Account/ForgotPassword");
        }

        Input.Email = email;
        Input.Token = token;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _users.ResetPasswordAsync(Input.Email, Input.Token, Input.Password, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ToVietnamesePasswordError(ex.Message));
            return Page();
        }

        TempData["AuthSuccess"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";
        return RedirectToPage("/Account/Login");
    }

    private static string ToVietnamesePasswordError(string message)
    {
        if (message.Contains("invalid or expired", StringComparison.OrdinalIgnoreCase))
        {
            return "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
        }

        if (message.Contains("at least 8 characters", StringComparison.OrdinalIgnoreCase))
        {
            return "Mật khẩu phải có ít nhất 8 ký tự.";
        }

        return string.IsNullOrWhiteSpace(message) ? "Không thể đặt lại mật khẩu." : message;
    }
}
