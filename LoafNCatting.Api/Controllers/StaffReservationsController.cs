using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/staff/reservations")]
public class StaffReservationsController(
    IReservationService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReservationDto>>> GetReservations(
        [FromQuery(Name = "status")] int? status,
        [FromQuery] DateOnly? date)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetStaffReservationsAsync(status, date));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ReservationDto>> UpdateStatus(
        int id,
        StaffReservationStatusDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        var reservation = await service.UpdateReservationStatusAsync(id, request);
        return reservation is null
            ? BadRequest(new { message = "Reservation was not found or status transition is invalid." })
            : Ok(reservation);
    }
}
