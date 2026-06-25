using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService service, ISessionTokenService sessions) : ControllerBase
{
    // Tạo link/QR thanh toán PayOS cho một đơn đang chờ thanh toán.
    [HttpPost("create-link")]
    public async Task<ActionResult<PaymentLinkDto>> CreateLink(CreatePaymentLinkRequestDto request)
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out var session, out var failure))
        {
            return failure!;
        }

        var link = await service.CreatePaymentLinkAsync(request.OrderId, session!.UserId);
        return link is null
            ? BadRequest(new { message = "Cannot create payment link. Order not found or already paid." })
            : Ok(link);
    }

    // Flutter poll endpoint này để biết đơn đã thanh toán chưa.
    [HttpGet("status/{orderId:int}")]
    public async Task<ActionResult<PaymentStatusDto>> GetStatus(int orderId)
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out var session, out var failure))
        {
            return failure!;
        }

        var status = await service.GetPaymentStatusAsync(orderId, session!.UserId);
        return status is null ? NotFound() : Ok(status);
    }
}
