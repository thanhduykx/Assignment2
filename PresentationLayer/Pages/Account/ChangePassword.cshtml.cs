using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Services;

namespace PresentationLayer.Pages.Account;

[Authorize]
public sealed class ChangePasswordModel : PageModel
{
    private readonly IUserAccountStore _users;

    public ChangePasswordModel(IUserAccountStore users)
    {
        _users = users;
    }

    [BindProperty]
    public ChangePasswordViewModel Input { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
        StatusMessage = TempData["AccountSuccess"] as string;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        try
        {
            await _users.ChangePasswordAsync(userId, Input.CurrentPassword, Input.NewPassword, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ToVietnamesePasswordError(ex.Message));
            return Page();
        }

        TempData["AccountSuccess"] = "Đổi mật khẩu thành công.";
        return RedirectToPage();
    }

    private static string ToVietnamesePasswordError(string message)
    {
        if (message.Contains("does not have a local password", StringComparison.OrdinalIgnoreCase))
        {
            return "Tài khoản này chưa có mật khẩu nội bộ. Dùng chức năng quên mật khẩu để tạo mật khẩu.";
        }

        if (message.Contains("Current password is incorrect", StringComparison.OrdinalIgnoreCase))
        {
            return "Mật khẩu hiện tại không đúng.";
        }

        if (message.Contains("at least 8 characters", StringComparison.OrdinalIgnoreCase))
        {
            return "Mật khẩu mới phải có ít nhất 8 ký tự.";
        }

        return string.IsNullOrWhiteSpace(message) ? "Không thể đổi mật khẩu." : message;
    }
}
