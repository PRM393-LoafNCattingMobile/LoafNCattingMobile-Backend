using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Auth;
using LoafNCatting.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(INotificationService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<NotificationDto>>> GetUserNotifications(int userId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetUserNotificationsAsync(userId));
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out var session, out var failure))
        {
            return failure!;
        }

        return await service.MarkNotificationReadAsync(id, session!.UserId) ? NoContent() : NotFound();
    }
}


