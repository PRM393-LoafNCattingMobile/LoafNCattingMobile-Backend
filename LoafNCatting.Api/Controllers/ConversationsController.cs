using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Auth;
using LoafNCatting.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController(IConversationService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ConversationDto>> GetUserConversation(int userId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetOrCreateConversationAsync(userId));
    }
}


