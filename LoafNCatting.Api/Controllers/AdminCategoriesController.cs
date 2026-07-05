using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
public class AdminCategoriesController(
    ICategoryService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetCategoriesAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var category = await service.GetCategoryAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(AdminCategoryRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var category = await service.CreateCategoryAsync(request);
        return category is null ? BadRequest(new { message = "Category data is invalid." }) : Ok(category);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, AdminCategoryRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var category = await service.UpdateCategoryAsync(id, request);
        return category is null ? BadRequest(new { message = "Category data is invalid or category was not found." }) : Ok(category);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return await service.DeleteCategoryAsync(id)
            ? NoContent()
            : BadRequest(new { message = "Category was not found or still has products." });
    }
}
