using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/staff/products")]
public class StaffProductsController(
    IProductService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpPut("{id:int}/availability")]
    public async Task<ActionResult<ProductDto>> UpdateAvailability(int id, StaffProductAvailabilityDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var product = await service.UpdateAvailabilityAsync(id, request);
        return product is null
            ? BadRequest(new { message = "Product was not found or availability data is invalid." })
            : Ok(product);
    }
}
