using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Auth;
using LoafNCatting.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(IMessageService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpGet("conversation/{conversationId:int}")]
    public async Task<ActionResult<List<MessageDto>>> GetConversationMessages(int conversationId)
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out var session, out var failure))
        {
            return failure!;
        }

        var messages = await service.GetMessagesAsync(conversationId, session!.UserId);
        return messages is null ? Forbid() : Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<List<MessageDto>>> SendMessage(CreateMessageDto request)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, request.SenderUserId, out var failure))
        {
            return failure!;
        }

        var messages = await service.SendMessageAsync(request, request.SenderUserId);
        return messages is null ? Forbid() : Ok(messages);
    }
}


