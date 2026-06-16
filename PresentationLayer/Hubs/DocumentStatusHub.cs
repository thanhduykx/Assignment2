using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Security;
using PresentationLayer.Services;

namespace PresentationLayer.Hubs;

[Authorize]
public sealed class DocumentStatusHub : Hub
{
    public const string DocumentStatusChangedEvent = "documentStatusChanged";
    public const string DocumentIndexProgressChangedEvent = "documentIndexProgressChanged";
    public const string OnlineUsersChangedEvent = "onlineUsersChanged";
    public const string AdminGroup = "documents:admins";

    private readonly IOnlineUserPresenceTracker _onlineUserPresenceTracker;

    public DocumentStatusHub(IOnlineUserPresenceTracker onlineUserPresenceTracker)
    {
        _onlineUserPresenceTracker = onlineUserPresenceTracker;
    }

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

        var snapshot = _onlineUserPresenceTracker.RegisterConnection(Context.ConnectionId, Context.User);
        await Clients.All.SendAsync(OnlineUsersChangedEvent, snapshot);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var snapshot = _onlineUserPresenceTracker.UnregisterConnection(Context.ConnectionId);
        await Clients.All.SendAsync(OnlineUsersChangedEvent, snapshot);
        await base.OnDisconnectedAsync(exception);
    }

    public Task<OnlineUsersChangedPayload> GetOnlineUsersAsync()
    {
        return Task.FromResult(_onlineUserPresenceTracker.GetSnapshot());
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
