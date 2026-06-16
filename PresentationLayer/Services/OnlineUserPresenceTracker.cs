using System.Collections.Concurrent;
using System.Security.Claims;
using PresentationLayer.Security;

namespace PresentationLayer.Services;

public sealed record OnlineUserSummaryPayload(
    string UserKey,
    string UserId,
    string DisplayName,
    string Email,
    string Role,
    string Initials,
    int ConnectionCount);

public sealed record OnlineUsersChangedPayload(
    int OnlineUserCount,
    IReadOnlyList<OnlineUserSummaryPayload> Users,
    DateTimeOffset UpdatedAt,
    string WindowLabel);

public interface IOnlineUserPresenceTracker
{
    OnlineUsersChangedPayload RegisterConnection(string connectionId, ClaimsPrincipal? user);
    OnlineUsersChangedPayload UnregisterConnection(string connectionId);
    OnlineUsersChangedPayload GetSnapshot();
}

public sealed class InMemoryOnlineUserPresenceTracker : IOnlineUserPresenceTracker
{
    private const string ActiveWindowLabel = "active now";

    private readonly ConcurrentDictionary<string, OnlineConnectionPresence> _connections = new(StringComparer.Ordinal);

    public OnlineUsersChangedPayload RegisterConnection(string connectionId, ClaimsPrincipal? user)
    {
        var presence = OnlineConnectionPresence.From(connectionId, user);
        _connections[connectionId] = presence;
        return BuildSnapshot();
    }

    public OnlineUsersChangedPayload UnregisterConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        return BuildSnapshot();
    }

    public OnlineUsersChangedPayload GetSnapshot()
    {
        return BuildSnapshot();
    }

    private OnlineUsersChangedPayload BuildSnapshot()
    {
        var users = _connections.Values
            .GroupBy(item => item.UserKey, StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                return new OnlineUserSummaryPayload(
                    first.UserKey,
                    first.UserId,
                    first.DisplayName,
                    first.Email,
                    first.Role,
                    first.Initials,
                    group.Count());
            })
            .OrderByDescending(item => RoleRank(item.Role))
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new OnlineUsersChangedPayload(
            users.Count,
            users,
            DateTimeOffset.UtcNow,
            ActiveWindowLabel);
    }

    private static int RoleRank(string role)
    {
        return AppRoles.Normalize(role) switch
        {
            AppRoles.Admin => 3,
            AppRoles.Lecturer => 2,
            AppRoles.Student => 1,
            _ => 0
        };
    }

    private sealed record OnlineConnectionPresence(
        string ConnectionId,
        string UserKey,
        string UserId,
        string DisplayName,
        string Email,
        string Role,
        string Initials)
    {
        public static OnlineConnectionPresence From(string connectionId, ClaimsPrincipal? user)
        {
            var rawUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier)?.Trim() ?? string.Empty;
            var email = user?.FindFirstValue(ClaimTypes.Email)?.Trim() ?? string.Empty;
            var displayName = user?.FindFirstValue(ClaimTypes.Name)?.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = !string.IsNullOrWhiteSpace(email)
                    ? email.Split('@')[0]
                    : "Unknown user";
            }

            var role = AppRoles.Normalize(user?.FindFirstValue(ClaimTypes.Role));
            var userKey = !string.IsNullOrWhiteSpace(rawUserId)
                ? rawUserId
                : !string.IsNullOrWhiteSpace(email)
                    ? email.ToUpperInvariant()
                    : connectionId;

            return new OnlineConnectionPresence(
                connectionId,
                userKey,
                rawUserId,
                displayName,
                email,
                role,
                BuildInitials(displayName));
        }

        private static string BuildInitials(string displayName)
        {
            var parts = displayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Take(2)
                .ToArray();

            if (parts.Length == 0)
            {
                return "U";
            }

            return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}
