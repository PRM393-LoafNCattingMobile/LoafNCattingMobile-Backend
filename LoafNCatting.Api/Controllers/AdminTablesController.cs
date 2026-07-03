using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/admin/tables")]
public class AdminTablesController(
    ITableService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TableDto>>> GetTables()
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetTablesAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableDto>> GetTable(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var table = await service.GetTableAsync(id);
        return table is null ? NotFound() : Ok(table);
    }

    [HttpPost]
    public async Task<ActionResult<TableDto>> CreateTable(AdminTableRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var table = await service.CreateTableAsync(request);
        return table is null ? BadRequest(new { message = "Table data is invalid." }) : Ok(table);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TableDto>> UpdateTable(int id, AdminTableRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var table = await service.UpdateTableAsync(id, request);
        return table is null ? BadRequest(new { message = "Table data is invalid or table was not found." }) : Ok(table);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTable(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return await service.DeleteTableAsync(id) ? NoContent() : NotFound();
    }
}
