using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/admin/store-location")]
public class AdminStoreLocationController(
    IStoreLocationService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult<StoreLocationDto>> UpdateStoreLocation(
        AdminStoreLocationRequestDto request)
    {
        if (!SessionAuthorization.TryRequireAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        var location = await service.UpdateStoreLocationAsync(request);
        return location is null
            ? BadRequest(new { message = "Store location was not found or data is invalid." })
            : Ok(location);
    }
}
