using LoafNCatting.Api.Hubs;
using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/staff/conversations")]
public class StaffConversationsController(
    IConversationService conversations,
    IMessageService messages,
    ISessionTokenService sessions,
    IHubContext<SupportChatHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ConversationInboxItemDto>>> GetInbox()
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return Ok(await conversations.GetInboxAsync());
    }

    [HttpGet("{conversationId:int}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(int conversationId)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var result = await messages.GetMessagesForSupportAsync(conversationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{conversationId:int}/messages")]
    public async Task<ActionResult<List<MessageDto>>> SendMessage(
        int conversationId,
        SupportMessageDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out var session, out var failure))
        {
            return failure!;
        }

        var result = await messages.SendSupportMessageAsync(conversationId, request, session!.UserId);
        if (result is null)
        {
            return NotFound();
        }

        await hub.Clients.Group(SupportChatHub.ConversationGroup(conversationId))
            .SendAsync(SupportChatHub.ThreadUpdatedEvent, result);
        await hub.Clients.Group(SupportChatHub.StaffInboxGroup)
            .SendAsync(SupportChatHub.InboxUpdatedEvent, conversationId);

        return Ok(result);
    }
}
