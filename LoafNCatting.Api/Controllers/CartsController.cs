using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartsController(ICartService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<CartDto>> GetUserCart(int userId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetCartAsync(userId));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(CartItemRequestDto request)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, request.UserId, out var failure))
        {
            return failure!;
        }

        var cart = await service.AddItemAsync(request);
        return cart is null ? BadRequest(new { message = "Product is unavailable or out of stock." }) : Ok(cart);
    }

    [HttpPut("items")]
    public async Task<ActionResult<CartDto>> UpdateItem(CartItemRequestDto request)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, request.UserId, out var failure))
        {
            return failure!;
        }

        var cart = await service.UpdateItemAsync(request);
        return cart is null ? BadRequest(new { message = "Product is unavailable or out of stock." }) : Ok(cart);
    }

    [HttpDelete("user/{userId:int}/items/{productId:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int userId, int productId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.RemoveItemAsync(userId, productId));
    }

    [HttpDelete("user/{userId:int}")]
    public async Task<ActionResult<CartDto>> ClearCart(int userId)
    {
        if (!SessionAuthorization.TryRequireUserSession(Request, sessions, userId, out var failure))
        {
            return failure!;
        }

        return Ok(await service.ClearCartAsync(userId));
    }
}
