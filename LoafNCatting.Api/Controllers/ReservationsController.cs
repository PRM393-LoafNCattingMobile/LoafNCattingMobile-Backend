using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Auth;
using LoafNCatting.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IReservationService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto request)
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out var session, out var failure))
        {
            return failure!;
        }

        if (request.UserId != session!.UserId)
        {
            return new ObjectResult(new { message = "Session does not match the requested user." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var reservation = await service.CreateReservationAsync(request);
        return reservation is null ? BadRequest(new { message = "Table is not available for the selected time." }) : Ok(reservation);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<ReservationDto>>> GetUserReservations(int userId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetUserReservationsAsync(userId));
    }
}


