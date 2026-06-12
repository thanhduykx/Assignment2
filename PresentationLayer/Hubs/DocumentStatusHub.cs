using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Security;

namespace PresentationLayer.Hubs;

[Authorize(Policy = AuthorizationPolicies.DocumentManagement)]
public sealed class DocumentStatusHub : Hub
{
    public const string DocumentStatusChangedEvent = "documentStatusChanged";
    public const string AdminGroup = "documents:admins";

    public override async Task OnConnectedAsync()
    {
        var role = AppRoles.Normalize(Context.User?.FindFirstValue(ClaimTypes.Role));
        var groupTasks = new List<Task>();

        if (role == AppRoles.Admin)
        {
            groupTasks.Add(Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup));
        }

        if (role == AppRoles.Lecturer)
        {
            if (Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                groupTasks.Add(Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId)));
            }

            var email = Context.User?.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                groupTasks.Add(Groups.AddToGroupAsync(Context.ConnectionId, EmailGroup(email)));
            }
        }

        if (groupTasks.Count > 0)
        {
            await Task.WhenAll(groupTasks);
        }

        await base.OnConnectedAsync();
    }

    public static string UserGroup(Guid userId)
    {
        return $"documents:user:{userId:N}";
    }

    public static string EmailGroup(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail));
        return $"documents:email:{Convert.ToHexString(hash)}";
    }
}
