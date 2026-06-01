using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(IMessageService service) : ControllerBase
{
    [HttpGet("conversation/{conversationId:int}")]
    public async Task<ActionResult<List<MessageDto>>> GetConversationMessages(int conversationId)
    {
        return Ok(await service.GetMessagesAsync(conversationId));
    }

    [HttpPost]
    public async Task<ActionResult<List<MessageDto>>> SendMessage(CreateMessageDto request)
    {
        return Ok(await service.SendMessageAsync(request));
    }
}


