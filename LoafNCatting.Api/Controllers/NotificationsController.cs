using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<NotificationDto>>> GetUserNotifications(int userId)
    {
        return Ok(await service.GetUserNotificationsAsync(userId));
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        return await service.MarkNotificationReadAsync(id) ? NoContent() : NotFound();
    }
}


