using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/store-location")]
public class StoreLocationController(IStoreLocationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StoreLocationDto>> GetStoreLocation()
    {
        var location = await service.GetStoreLocationAsync();
        return location is null ? NotFound() : Ok(location);
    }
}


