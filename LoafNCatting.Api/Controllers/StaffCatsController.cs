using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/staff/cats")]
public class StaffCatsController(
    ICatService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<CatDto>> UpdateStatus(int id, StaffCatStatusDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var cat = await service.UpdateCatStatusAsync(id, request);
        return cat is null
            ? BadRequest(new { message = "Cat was not found or status data is invalid." })
            : Ok(cat);
    }
}
