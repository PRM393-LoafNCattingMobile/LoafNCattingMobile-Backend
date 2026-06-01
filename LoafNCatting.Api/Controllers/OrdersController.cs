using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequestDto request)
    {
        var order = await service.CreateOrderAsync(request);
        return order is null ? BadRequest(new { message = "Order could not be created. Check products and quantities." }) : Ok(order);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<OrderDto>>> GetUserOrders(int userId)
    {
        return Ok(await service.GetUserOrdersAsync(userId));
    }
}


