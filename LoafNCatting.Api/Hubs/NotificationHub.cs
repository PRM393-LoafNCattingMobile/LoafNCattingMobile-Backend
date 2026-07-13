using LoafNCatting.Service.Auth;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.Api.Hubs;

public class NotificationHub(ISessionTokenService sessions) : Hub
{
    public const string NotificationCreatedEvent = "NotificationCreated";

    public static string UserGroup(int userId) => $"notifications:user:{userId}";

    public Task JoinUserNotifications()
    {
        var session = RequireSession();
        return Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(session.UserId));
    }

    private UserSession RequireSession()
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new HubException("Missing session token.");
        }

        return sessions.GetSession(token)
            ?? throw new HubException("Session is invalid or expired.");
    }

    private string? ReadToken()
    {
        var httpContext = Context.GetHttpContext();
        var queryToken = httpContext?.Request.Query["access_token"].ToString();
        if (!string.IsNullOrWhiteSpace(queryToken))
        {
            return queryToken;
        }

        var headerToken = httpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(headerToken) &&
            headerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return headerToken["Bearer ".Length..].Trim();
        }

        return httpContext?.Request.Headers["X-Session-Token"].ToString();
    }
}
