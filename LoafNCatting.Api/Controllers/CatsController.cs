using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatsController(ICatService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CatDto>>> GetCats([FromQuery] string? search)
    {
        return Ok(await service.GetCatsAsync(search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatDto>> GetCat(int id)
    {
        var cat = await service.GetCatAsync(id);
        return cat is null ? NotFound() : Ok(cat);
    }
}


