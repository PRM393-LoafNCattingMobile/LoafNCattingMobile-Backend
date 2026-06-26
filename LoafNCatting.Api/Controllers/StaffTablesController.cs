using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/staff/tables")]
public class StaffTablesController(
    ITableService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TableDto>> UpdateStatus(int id, StaffTableStatusDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var table = await service.UpdateTableStatusAsync(id, request);
        return table is null
            ? BadRequest(new { message = "Table was not found or status data is invalid." })
            : Ok(table);
    }
}
