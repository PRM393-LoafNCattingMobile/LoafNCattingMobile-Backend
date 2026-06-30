using LoafNCatting.Api.Controllers;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Service.Tests;

public class AdminCatalogControllerTests
{
    [Fact]
    public async Task AdminProducts_CreateProduct_ReturnsForbidden_ForStaff()
    {
        var controller = CreateAdminProductsController("Staff");

        var result = await controller.CreateProduct(SampleProductRequest());

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    [Fact]
    public async Task StaffProducts_UpdateAvailability_AllowsStaff()
    {
        var controller = CreateStaffProductsController("Staff");

        var result = await controller.UpdateAvailability(
            7,
            new StaffProductAvailabilityDto(UnitInStock: 0, IsAvailable: false));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var product = Assert.IsType<ProductDto>(ok.Value);
        Assert.Equal(0, product.UnitInStock);
        Assert.False(product.IsAvailable);
    }

    [Fact]
    public async Task AdminCategories_CreateCategory_ReturnsOk_ForAdmin()
    {
        var controller = CreateAdminCategoriesController("Admin");

        var result = await controller.CreateCategory(new AdminCategoryRequestDto("Trà", null));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var category = Assert.IsType<CategoryDto>(ok.Value);
        Assert.Equal("Trà", category.Name);
    }

    private static AdminProductsController CreateAdminProductsController(string roleName)
    {
        var controller = new AdminProductsController(
            new FakeProductService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static StaffProductsController CreateStaffProductsController(string roleName)
    {
        var controller = new StaffProductsController(
            new FakeProductService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static AdminCategoriesController CreateAdminCategoriesController(string roleName)
    {
        var controller = new AdminCategoriesController(
            new FakeCategoryService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static AdminProductRequestDto SampleProductRequest() =>
        new("Latte", null, 45000m, null, 5, null, CategoryId: 1, IsAvailable: true);

    private sealed class FakeProductService : IProductService
    {
        public Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search) => Task.FromResult<List<ProductDto>>([]);

        public Task<ProductDto?> GetProductAsync(int id) => Task.FromResult<ProductDto?>(SampleProduct(id));

        public Task<ProductDto?> CreateProductAsync(AdminProductRequestDto request) =>
            Task.FromResult<ProductDto?>(SampleProduct(7));

        public Task<ProductDto?> UpdateProductAsync(int id, AdminProductRequestDto request) =>
            Task.FromResult<ProductDto?>(SampleProduct(id));

        public Task<ProductDto?> UpdateAvailabilityAsync(int id, StaffProductAvailabilityDto request) =>
            Task.FromResult<ProductDto?>(new ProductDto(id, "Latte", null, 45000m, null, request.UnitInStock, null, 1, "Cà phê", request.IsAvailable, false));

        public Task<bool> DeleteProductAsync(int id) => Task.FromResult(true);

        private static ProductDto SampleProduct(int id) =>
            new(id, "Latte", null, 45000m, null, 5, null, 1, "Cà phê", true, true);
    }

    private sealed class FakeCategoryService : ICategoryService
    {
        public Task<List<CategoryDto>> GetCategoriesAsync() => Task.FromResult<List<CategoryDto>>([]);

        public Task<CategoryDto?> GetCategoryAsync(int id) => Task.FromResult<CategoryDto?>(new CategoryDto(id, "Trà", null));

        public Task<CategoryDto?> CreateCategoryAsync(AdminCategoryRequestDto request) =>
            Task.FromResult<CategoryDto?>(new CategoryDto(1, request.Name, request.Description));

        public Task<CategoryDto?> UpdateCategoryAsync(int id, AdminCategoryRequestDto request) =>
            Task.FromResult<CategoryDto?>(new CategoryDto(id, request.Name, request.Description));

        public Task<bool> DeleteCategoryAsync(int id) => Task.FromResult(true);
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(User user) => "test-token";

        public UserSession? GetSession(string token) => token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }
}
