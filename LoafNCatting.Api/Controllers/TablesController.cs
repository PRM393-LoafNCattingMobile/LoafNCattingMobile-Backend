using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TablesController(ITableService service) : ControllerBase
{
    [HttpGet("available")]
    public async Task<ActionResult<List<TableDto>>> GetAvailableTables([FromQuery] DateOnly date, [FromQuery] TimeOnly time, [FromQuery] int guestCount)
    {
        return Ok(await service.GetAvailableTablesAsync(date, time, guestCount));
    }
}


