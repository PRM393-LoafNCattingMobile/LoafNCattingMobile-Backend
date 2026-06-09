using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Auth;
using LoafNCatting.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequestDto request)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, request.UserId, out var failure))
        {
            return failure!;
        }

        var order = await service.CreateOrderAsync(request);
        return order is null ? BadRequest(new { message = "Order could not be created. Check products and quantities." }) : Ok(order);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<OrderDto>>> GetUserOrders(int userId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetUserOrdersAsync(userId));
    }
}


