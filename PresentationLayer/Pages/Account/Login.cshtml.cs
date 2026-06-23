using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;

namespace PresentationLayer.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    private const string AccountProvisioningMessage = "Tài khoản được cấp bởi Nhà trường. Vui lòng liên hệ Nhà trường để xin cấp tài khoản.";

    private readonly IUserAccountStore _users;
    private readonly IAuthenticationSchemeProvider _schemes;

    public LoginModel(IUserAccountStore users, IAuthenticationSchemeProvider schemes)
    {
        _users = users;
        _schemes = schemes;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool IsGoogleLoginEnabled { get; private set; }
    public string? GoogleLoginUnavailableMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultDashboard(User.FindFirstValue(ClaimTypes.Role));
        }

        ReturnUrl = returnUrl;
        await ConfigureGoogleLoginStateAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await ConfigureGoogleLoginStateAsync();
            return Page();
        }

        var user = await _users.FindByEmailAsync(Email, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, AccountProvisioningMessage);
            await ConfigureGoogleLoginStateAsync();
            return Page();
        }

        if (!_users.VerifyPassword(user, Password))
        {
            ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
            await ConfigureGoogleLoginStateAsync();
            return Page();
        }

        await SignInAsync(user);
        return RedirectAfterSignIn(user, ReturnUrl);
    }

    public async Task<IActionResult> OnPostGoogleLoginAsync(string? returnUrl = null)
    {
        if (await _schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is null)
        {
            TempData["AuthError"] = "Google sign-in is not configured.";
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        if (IsPrivateIpHost(Request.Host.Host))
        {
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Page("/Account/GoogleCallback", pageHandler: null, values: new { returnUrl }, protocol: Request.Scheme)
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
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

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
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

        return RedirectToDefaultDashboard(user.Role);
    }

    private IActionResult RedirectToDefaultDashboard(string? role)
    {
        return AppRoles.Normalize(role) switch
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

    private async Task ConfigureGoogleLoginStateAsync()
    {
        GoogleLoginUnavailableMessage = null;
        IsGoogleLoginEnabled = await _schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is not null;

        if (IsGoogleLoginEnabled && IsPrivateIpHost(Request.Host.Host))
        {
            IsGoogleLoginEnabled = false;
        }
    }

    private static bool IsPrivateIpHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        host = host.Trim('[', ']');
        if (!IPAddress.TryParse(host, out var ipAddress) || IPAddress.IsLoopback(ipAddress))
        {
            return false;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();
            return bytes[0] == 10
                   || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                   || (bytes[0] == 192 && bytes[1] == 168);
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ipAddress.GetAddressBytes();
            return ipAddress.IsIPv6LinkLocal || (bytes[0] & 0xfe) == 0xfc;
        }

        return false;
    }
}
