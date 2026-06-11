using Microsoft.AspNetCore.Authorization;
using PresentationLayer.Security;

namespace PresentationLayer.Pages.Home;

[Authorize(Policy = AuthorizationPolicies.ChatAccess)]
public sealed class PrivacyModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public void OnGet()
    {
    }
}
