using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/staff/orders")]
public class StaffOrdersController(
    IOrderService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(
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

        return Ok(await service.GetStaffOrdersAsync(status, date));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        var order = await service.GetStaffOrderAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        int id,
        StaffOrderStatusDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(
                Request,
                sessions,
                out var session,
                out var failure))
        {
            return failure!;
        }

        var order = await service.UpdateOrderStatusAsync(
            id,
            session!.UserId,
            request);
        return order is null
            ? BadRequest(new { message = "Order was not found or status transition is invalid." })
            : Ok(order);
    }
}
