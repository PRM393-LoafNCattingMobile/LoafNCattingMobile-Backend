using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController(IConversationService service) : ControllerBase
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ConversationDto>> GetUserConversation(int userId)
    {
        return Ok(await service.GetOrCreateConversationAsync(userId));
    }
}


