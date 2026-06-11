using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Services;

namespace PresentationLayer.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordModel : PageModel
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IUserAccountStore _users;
    private readonly IAccountEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        IUserAccountStore users,
        IAccountEmailSender emailSender,
        ILogger<ForgotPasswordModel> logger)
    {
        _users = users;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public ForgotPasswordViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var reset = await _users.CreatePasswordResetTokenAsync(Input.Email, ResetTokenLifetime, cancellationToken);
        if (reset is not null)
        {
            var resetUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { email = reset.Value.Account.Email, token = reset.Value.Token },
                protocol: Request.Scheme);

            try
            {
                await _emailSender.SendPasswordResetEmailAsync(
                    reset.Value.Account,
                    resetUrl ?? string.Empty,
                    reset.Value.ExpiresAt,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not send password reset email to {Email}", reset.Value.Account.Email);
                ModelState.AddModelError(string.Empty, "Không gửi được email đặt lại mật khẩu. Kiểm tra cấu hình SMTP rồi thử lại.");
                return Page();
            }
        }

        TempData["AuthSuccess"] = "Nếu email tồn tại, hệ thống đã gửi liên kết đặt lại mật khẩu.";
        return RedirectToPage("/Account/Login");
    }
}
