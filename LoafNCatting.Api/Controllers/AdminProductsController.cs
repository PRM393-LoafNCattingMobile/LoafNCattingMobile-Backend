using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
public class AdminProductsController(
    IProductService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts([FromQuery] int? categoryId, [FromQuery] string? search)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetProductsAsync(categoryId, search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var product = await service.GetProductAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(AdminProductRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var product = await service.CreateProductAsync(request);
        return product is null ? BadRequest(new { message = "Product data is invalid." }) : Ok(product);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, AdminProductRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var product = await service.UpdateProductAsync(id, request);
        return product is null ? BadRequest(new { message = "Product data is invalid or product was not found." }) : Ok(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return await service.DeleteProductAsync(id) ? NoContent() : NotFound();
    }
}
