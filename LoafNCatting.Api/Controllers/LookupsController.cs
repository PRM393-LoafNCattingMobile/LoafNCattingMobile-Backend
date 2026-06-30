using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupsController(
    ILookupService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet("admin")]
    public async Task<ActionResult<AdminLookupsDto>> GetAdminLookups()
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetAdminLookupsAsync());
    }
}
