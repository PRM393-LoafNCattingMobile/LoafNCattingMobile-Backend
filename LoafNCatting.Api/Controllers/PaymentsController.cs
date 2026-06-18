using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService service) : ControllerBase
{
    // Tạo link/QR thanh toán PayOS cho một đơn đang chờ thanh toán.
    [HttpPost("create-link")]
    public async Task<ActionResult<PaymentLinkDto>> CreateLink(CreatePaymentLinkRequestDto request)
    {
        var link = await service.CreatePaymentLinkAsync(request.OrderId);
        return link is null
            ? BadRequest(new { message = "Cannot create payment link. Order not found or already paid." })
            : Ok(link);
    }

    // Flutter poll endpoint này để biết đơn đã thanh toán chưa.
    [HttpGet("status/{orderId:int}")]
    public async Task<ActionResult<PaymentStatusDto>> GetStatus(int orderId)
    {
        var status = await service.GetPaymentStatusAsync(orderId);
        return status is null ? NotFound() : Ok(status);
    }
}
