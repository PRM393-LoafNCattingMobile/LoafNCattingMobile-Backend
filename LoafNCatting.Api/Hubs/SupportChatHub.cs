using LoafNCatting.Data.Interfaces;
using LoafNCatting.Service.Auth;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.Api.Hubs;

public class SupportChatHub(
    ISessionTokenService sessions,
    IConversationRepository conversations) : Hub
{
    public const string StaffInboxGroup = "staff-inbox";
    public const string ThreadUpdatedEvent = "ThreadUpdated";
    public const string InboxUpdatedEvent = "InboxUpdated";

    public static string ConversationGroup(int conversationId) => $"conversation:{conversationId}";

    public async Task JoinConversation(int conversationId)
    {
        var session = RequireSession();
        var conversation = await conversations.GetByIdAsync(conversationId)
            ?? throw new HubException("Conversation not found.");

        if (!IsSupport(session) && conversation.CustomerUserId != session.UserId)
        {
            throw new HubException("You are not allowed to access this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public Task JoinStaffInbox()
    {
        var session = RequireSession();
        if (!IsSupport(session))
        {
            throw new HubException("Only staff or admin can access the shared inbox.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, StaffInboxGroup);
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

    private static bool IsSupport(UserSession session) =>
        string.Equals(session.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(session.RoleName, "Staff", StringComparison.OrdinalIgnoreCase);
}
