using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IReservationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto request)
    {
        var reservation = await service.CreateReservationAsync(request);
        return reservation is null ? BadRequest(new { message = "Table is not available for the selected time." }) : Ok(reservation);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<ReservationDto>>> GetUserReservations(int userId)
    {
        return Ok(await service.GetUserReservationsAsync(userId));
    }
}


