using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;

namespace PresentationLayer.Pages.Account;

[AllowAnonymous]
public sealed class GoogleCallbackModel : PageModel
{
    private const string AccountProvisioningMessage = "Tài khoản được cấp bởi Nhà trường. Vui lòng liên hệ Nhà trường để xin cấp tài khoản.";

    private readonly IUserAccountStore _users;

    public GoogleCallbackModel(IUserAccountStore users)
    {
        _users = users;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded || result.Principal is null)
        {
            TempData["AuthError"] = "Google sign-in failed.";
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["AuthError"] = "Google did not return an email address for this account.";
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        var user = await _users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            TempData["AuthError"] = AccountProvisioningMessage;
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        await SignInAsync(user);
        await HttpContext.SignOutAsync("External");
        return RedirectAfterSignIn(user, returnUrl);
    }

    private async Task SignInAsync(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, AppRoles.Normalize(user.Role))
        };

        var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }

    private IActionResult RedirectAfterSignIn(UserAccount user, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && Url.IsLocalUrl(returnUrl)
            && CanAccessReturnUrl(AppRoles.Normalize(user.Role), returnUrl))
        {
            return Redirect(returnUrl);
        }

        return AppRoles.Normalize(user.Role) switch
        {
            AppRoles.Admin => RedirectToPage("/Home/Index"),
            AppRoles.Lecturer => RedirectToPage("/Home/Index"),
            _ => RedirectToPage("/Home/Chat")
        };
    }

    private static bool CanAccessReturnUrl(string role, string returnUrl)
    {
        if (role == AppRoles.Admin)
        {
            return true;
        }

        var path = returnUrl.Split('?', '#')[0].TrimEnd('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "/";
        }

        if (role == AppRoles.Lecturer)
        {
            return !path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase);
        }

        return path.Equals("/Home/Chat", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/Home/Chat/", StringComparison.OrdinalIgnoreCase);
    }
}
