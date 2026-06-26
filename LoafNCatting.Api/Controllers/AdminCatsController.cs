using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/admin/cats")]
public class AdminCatsController(
    ICatService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CatDto>>> GetCats([FromQuery] string? search)
    {
        if (!SessionAuthorization.TryRequireAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetCatsAsync(search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatDto>> GetCat(int id)
    {
        if (!SessionAuthorization.TryRequireAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var cat = await service.GetCatAsync(id);
        return cat is null ? NotFound() : Ok(cat);
    }

    [HttpPost]
    public async Task<ActionResult<CatDto>> CreateCat(AdminCatRequestDto request)
    {
        if (!SessionAuthorization.TryRequireAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var cat = await service.CreateCatAsync(request);
        return cat is null ? BadRequest(new { message = "Cat data is invalid." }) : Ok(cat);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CatDto>> UpdateCat(int id, AdminCatRequestDto request)
    {
        if (!SessionAuthorization.TryRequireAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var cat = await service.UpdateCatAsync(id, request);
        return cat is null ? BadRequest(new { message = "Cat data is invalid or cat was not found." }) : Ok(cat);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCat(int id)
    {
        if (!SessionAuthorization.TryRequireAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return await service.DeleteCatAsync(id) ? NoContent() : NotFound();
    }
}
